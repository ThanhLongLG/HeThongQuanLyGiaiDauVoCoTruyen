using System.Security.Cryptography;
using System.Text.Json;
using BaoCaoDACS;
using BaoCaoDACS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;

class Program
{
    private const float DefaultRating = 1000f;

    private sealed record EloState(float Rating, int MatchesPlayed);

    private sealed record ValidMatch(
        string MatchId,
        DateTime Date,
        int TournamentId,
        int LoaiHinhThiDauId,
        string HangCan,
        string VongDau,
        Participant Winner,
        Participant Loser);

    private sealed record MatchSamplePair(
        string MatchId,
        DateTime Date,
        MatchTrainingSample WinnerAsA,
        MatchTrainingSample LoserAsA);

    static void Main(string[] args)
    {
        var webProjectDirectory = FindWebProjectDirectory();
        var configPath = Path.Combine(
            Directory.GetParent(webProjectDirectory)!.FullName,
            "BaoCaoDACS.MLTrain",
            "appsettings.json");

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(configPath)!)
            .AddJsonFile(Path.GetFileName(configPath), optional: false)
            .Build();

        var connectionString = config.GetConnectionString("QLTAPVO")
            ?? throw new InvalidOperationException("Không tìm thấy connection string QLTAPVO.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var context = new AppDbContext(options);
        Console.WriteLine("Kết nối DB thành công.");

        var candidateMatches = context.match
            .AsNoTracking()
            .Include(m => m.Socre)
                .ThenInclude(s => s.participant)
            .Where(m => m.Socre.Count == 2 && m.Socre.All(s => s.Kq != null))
            .OrderBy(m => m.Date)
            .ThenBy(m => m.MatchId)
            .ToList();

        var rejectedReasons = new Dictionary<string, int>();
        var validMatches = candidateMatches
            .Select(match => ValidateMatch(match, rejectedReasons))
            .Where(match => match is not null)
            .Cast<ValidMatch>()
            .ToList();

        Console.WriteLine($"Trận ứng viên: {candidateMatches.Count}; hợp lệ: {validMatches.Count}; loại: {candidateMatches.Count - validMatches.Count}.");
        foreach (var reason in rejectedReasons.OrderBy(item => item.Key))
        {
            Console.WriteLine($"  - {reason.Key}: {reason.Value}");
        }

        if (validMatches.Count < 10)
        {
            Console.WriteLine("Không đủ tối thiểu 10 trận hợp lệ để train và đánh giá.");
            return;
        }

        // Tái dựng Elo theo thời gian. Mỗi mẫu chỉ được dùng rating trước trận,
        // thay vì lấy rating hiện tại trong TournamentRankings.
        var samplePairs = BuildChronologicalSamples(validMatches);

        // Chia theo nguyên trận và theo thời gian. Hai chiều A/B của cùng một trận
        // không bao giờ nằm ở hai tập khác nhau.
        var testMatchCount = Math.Max(1, (int)Math.Ceiling(samplePairs.Count * 0.2));
        var trainMatchCount = samplePairs.Count - testMatchCount;
        var trainPairs = samplePairs.Take(trainMatchCount).ToList();
        var testPairs = samplePairs.Skip(trainMatchCount).ToList();

        // Tập train dùng hai chiều để mô hình học tính đối xứng.
        var evaluationTrainSamples = trainPairs
            .SelectMany(pair => new[] { pair.WinnerAsA, pair.LoserAsA })
            .ToList();

        // Tập test chỉ dùng một chiều mỗi trận, xen kẽ nhãn để số đo không bị
        // nhân đôi giả tạo bởi bản sao đảo A/B.
        var evaluationTestSamples = testPairs
            .Select((pair, index) => index % 2 == 0 ? pair.WinnerAsA : pair.LoserAsA)
            .ToList();

        Console.WriteLine($"Train: {trainPairs.Count} trận / {evaluationTrainSamples.Count} mẫu.");
        Console.WriteLine($"Test thời gian: {testPairs.Count} trận / {evaluationTestSamples.Count} mẫu.");
        Console.WriteLine($"Nhãn test: thắng={evaluationTestSamples.Count(x => x.AWins)}, thua={evaluationTestSamples.Count(x => !x.AWins)}.");

        var mlContext = new MLContext(seed: 1);
        var pipeline = BuildPipeline(mlContext);

        var trainData = mlContext.Data.LoadFromEnumerable(evaluationTrainSamples);
        var testData = mlContext.Data.LoadFromEnumerable(evaluationTestSamples);
        var evaluationModel = pipeline.Fit(trainData);
        var predictions = evaluationModel.Transform(testData);
        var metrics = mlContext.BinaryClassification.Evaluate(
            predictions,
            labelColumnName: nameof(MatchTrainingSample.AWins));

        Console.WriteLine($"Accuracy : {metrics.Accuracy:0.####}");
        Console.WriteLine($"AUC      : {metrics.AreaUnderRocCurve:0.####}");
        Console.WriteLine($"F1       : {metrics.F1Score:0.####}");
        Console.WriteLine($"Precision: {metrics.PositivePrecision:0.####}");
        Console.WriteLine($"Recall   : {metrics.PositiveRecall:0.####}");
        Console.WriteLine($"LogLoss  : {metrics.LogLoss:0.####}");

        // Sau khi đánh giá đúng cách, train model triển khai trên toàn bộ lịch sử hợp lệ.
        var allSamples = samplePairs
            .SelectMany(pair => new[] { pair.WinnerAsA, pair.LoserAsA })
            .ToList();
        var allData = mlContext.Data.LoadFromEnumerable(allSamples);
        var finalModel = pipeline.Fit(allData);

        var modelDirectory = Path.Combine(webProjectDirectory, "wwwroot", "Models");
        Directory.CreateDirectory(modelDirectory);

        var trainedAtUtc = DateTime.UtcNow;
        var activeModelPath = Path.Combine(modelDirectory, "match_predictor.zip");

        mlContext.Model.Save(finalModel, allData.Schema, activeModelPath);

        var reloadedModel = mlContext.Model.Load(activeModelPath, out _);
        var predictionEngine = mlContext.Model.CreatePredictionEngine<MatchTrainingSample, MatchPredictionOutput>(reloadedModel);
        var smokePrediction = predictionEngine.Predict(allSamples[0]);
        if (!float.IsFinite(smokePrediction.Probability) || smokePrediction.Probability is < 0f or > 1f)
            throw new InvalidDataException("Model đã lưu không tạo được xác suất hợp lệ.");

        var modelHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(activeModelPath)));
        var report = new
        {
            trainedAtUtc,
            data = new
            {
                candidateMatches = candidateMatches.Count,
                validMatches = validMatches.Count,
                rejectedMatches = candidateMatches.Count - validMatches.Count,
                rejectedReasons,
                firstMatchUtc = validMatches.First().Date,
                lastMatchUtc = validMatches.Last().Date,
                tournaments = validMatches.Select(match => match.TournamentId).Distinct().Count()
            },
            split = new
            {
                strategy = "chronological-by-match",
                trainMatches = trainPairs.Count,
                testMatches = testPairs.Count,
                trainRows = evaluationTrainSamples.Count,
                testRows = evaluationTestSamples.Count
            },
            metrics = new
            {
                metrics.Accuracy,
                metrics.AreaUnderRocCurve,
                metrics.F1Score,
                metrics.PositivePrecision,
                metrics.PositiveRecall,
                metrics.NegativePrecision,
                metrics.NegativeRecall,
                metrics.LogLoss
            },
            finalTrainingRows = allSamples.Count,
            model = new
            {
                trainer = "LbfgsLogisticRegression",
                activeFile = Path.GetFileName(activeModelPath),
                sha256 = modelHash
            }
        };

        var reportPath = Path.Combine(modelDirectory, "match_predictor.metrics.json");
        File.WriteAllText(
            reportPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Đã triển khai model mới: {activeModelPath}");
        Console.WriteLine($"Kiểm tra nạp model thành công; xác suất mẫu thử: {smokePrediction.Probability:P2}");
        Console.WriteLine($"Báo cáo đánh giá: {reportPath}");
        Console.WriteLine($"SHA256: {modelHash}");
    }

    private static ValidMatch? ValidateMatch(Match match, IDictionary<string, int> rejectedReasons)
    {
        if (match.TournamentID is null)
            return Reject("Thiếu TournamentID");

        var scores = match.Socre.ToList();
        var winners = scores.Where(score => score.Kq == 1).ToList();
        var losers = scores.Where(score => score.Kq == 0).ToList();
        if (winners.Count != 1 || losers.Count != 1)
            return Reject("Kết quả không có đúng 1 thắng và 1 thua");

        var winner = winners[0].participant;
        var loser = losers[0].participant;
        if (winner is null || loser is null)
            return Reject("Thiếu võ sĩ");
        if (winner.ParticipantID == loser.ParticipantID)
            return Reject("Một võ sĩ xuất hiện hai lần");
        if (string.IsNullOrWhiteSpace(winner.UserId) || string.IsNullOrWhiteSpace(loser.UserId))
            return Reject("Thiếu UserId");
        if (!HasValidBiometrics(winner) || !HasValidBiometrics(loser))
            return Reject("Chỉ số cơ thể thiếu hoặc ngoài phạm vi hợp lý");

        return new ValidMatch(
            match.MatchId,
            match.Date,
            match.TournamentID.Value,
            match.LoaiHinhThiDauId,
            match.Hangcan?.Trim() ?? "Không xác định",
            match.Vongdau?.Trim() ?? "Không xác định",
            winner,
            loser);

        ValidMatch? Reject(string reason)
        {
            rejectedReasons.TryGetValue(reason, out var currentCount);
            rejectedReasons[reason] = currentCount + 1;
            return null;
        }
    }

    private static bool HasValidBiometrics(Participant participant) =>
        participant.CanNang is >= 30 and <= 200
        && participant.ChieuCao is >= 100 and <= 230
        && participant.tuoi is >= 8 and <= 80;

    private static List<MatchSamplePair> BuildChronologicalSamples(IEnumerable<ValidMatch> matches)
    {
        var ratings = new Dictionary<(int TournamentId, string UserId), EloState>();
        var result = new List<MatchSamplePair>();

        foreach (var match in matches.OrderBy(item => item.Date).ThenBy(item => item.MatchId))
        {
            var winnerKey = (match.TournamentId, match.Winner.UserId!);
            var loserKey = (match.TournamentId, match.Loser.UserId!);
            var winnerState = ratings.GetValueOrDefault(winnerKey, new EloState(DefaultRating, 0));
            var loserState = ratings.GetValueOrDefault(loserKey, new EloState(DefaultRating, 0));

            result.Add(new MatchSamplePair(
                match.MatchId,
                match.Date,
                CreateSample(match, match.Winner, match.Loser, winnerState.Rating, loserState.Rating, true),
                CreateSample(match, match.Loser, match.Winner, loserState.Rating, winnerState.Rating, false)));

            var expectedWinner = 1d / (1d + Math.Pow(10d, (loserState.Rating - winnerState.Rating) / 400d));
            var expectedLoser = 1d - expectedWinner;
            var winnerK = winnerState.MatchesPlayed < 10 ? 40f : 20f;
            var loserK = loserState.MatchesPlayed < 10 ? 40f : 20f;

            ratings[winnerKey] = new EloState(
                winnerState.Rating + winnerK * (float)(1d - expectedWinner),
                winnerState.MatchesPlayed + 1);
            ratings[loserKey] = new EloState(
                loserState.Rating + loserK * (float)(0d - expectedLoser),
                loserState.MatchesPlayed + 1);
        }

        return result;
    }

    private static MatchTrainingSample CreateSample(
        ValidMatch match,
        Participant fighterA,
        Participant fighterB,
        float fighterARating,
        float fighterBRating,
        bool aWins) =>
        new()
        {
            FighterA_Weight = fighterA.CanNang!.Value,
            FighterA_Height = fighterA.ChieuCao!.Value,
            FighterA_Age = fighterA.tuoi!.Value,
            FighterB_Weight = fighterB.CanNang!.Value,
            FighterB_Height = fighterB.ChieuCao!.Value,
            FighterB_Age = fighterB.tuoi!.Value,
            FighterA_Rating = fighterARating,
            FighterB_Rating = fighterBRating,
            RatingDiff = fighterARating - fighterBRating,
            LoaiHinhThiDauId = match.LoaiHinhThiDauId,
            HangCan = match.HangCan,
            VongDau = match.VongDau,
            DiffWeight = fighterA.CanNang.Value - fighterB.CanNang.Value,
            DiffHeight = fighterA.ChieuCao.Value - fighterB.ChieuCao.Value,
            DiffAge = fighterA.tuoi.Value - fighterB.tuoi.Value,
            AWins = aWins
        };

    private static IEstimator<ITransformer> BuildPipeline(MLContext mlContext) =>
        mlContext.Transforms.Categorical.OneHotEncoding(
                new[]
                {
                    new InputOutputColumnPair("HangCanEncoded", nameof(MatchTrainingSample.HangCan)),
                    new InputOutputColumnPair("VongDauEncoded", nameof(MatchTrainingSample.VongDau)),
                    new InputOutputColumnPair("LoaiHinhEncoded", nameof(MatchTrainingSample.LoaiHinhThiDauId))
                })
            .Append(mlContext.Transforms.Conversion.ConvertType(
                new[]
                {
                    new InputOutputColumnPair("FighterA_Age_f", nameof(MatchTrainingSample.FighterA_Age)),
                    new InputOutputColumnPair("FighterB_Age_f", nameof(MatchTrainingSample.FighterB_Age))
                },
                DataKind.Single))
            .Append(mlContext.Transforms.Concatenate(
                "Features",
                nameof(MatchTrainingSample.FighterA_Weight),
                nameof(MatchTrainingSample.FighterA_Height),
                "FighterA_Age_f",
                nameof(MatchTrainingSample.FighterB_Weight),
                nameof(MatchTrainingSample.FighterB_Height),
                "FighterB_Age_f",
                nameof(MatchTrainingSample.FighterA_Rating),
                nameof(MatchTrainingSample.FighterB_Rating),
                nameof(MatchTrainingSample.RatingDiff),
                nameof(MatchTrainingSample.DiffWeight),
                nameof(MatchTrainingSample.DiffHeight),
                nameof(MatchTrainingSample.DiffAge),
                "LoaiHinhEncoded",
                "HangCanEncoded",
                "VongDauEncoded"))
            .Append(mlContext.Transforms.NormalizeMeanVariance("Features"))
            .Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                labelColumnName: nameof(MatchTrainingSample.AWins),
                featureColumnName: "Features"));

    private static string FindWebProjectDirectory()
    {
        foreach (var startingPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(startingPath); directory is not null; directory = directory.Parent)
            {
                var nestedProject = Path.Combine(directory.FullName, "BaoCaoDACS", "BaoCaoDACS.csproj");
                if (File.Exists(nestedProject))
                    return Path.GetDirectoryName(nestedProject)!;

                var currentProject = Path.Combine(directory.FullName, "BaoCaoDACS.csproj");
                if (directory.Name == "BaoCaoDACS" && File.Exists(currentProject))
                    return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Không tìm thấy thư mục dự án web BaoCaoDACS.");
    }
}
