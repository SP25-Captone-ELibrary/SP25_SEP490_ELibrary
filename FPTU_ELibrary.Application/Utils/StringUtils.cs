using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Dtos.AIServices;
using MimeKit.Tnef;
using Org.BouncyCastle.Asn1.X509;

namespace FPTU_ELibrary.Application.Utils
{
    // Summary:
    //		Provide utility procedures to handle any logic related to String datatype
    public static class StringUtils
    {
        private static readonly Random _rnd = new Random();

        // Generate unique code with specific length
        public static string GenerateUniqueCode(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_rnd.Next(s.Length)])
                .ToArray());
        }
        
        // Generate random code digits
        public static int GenerateRandomCodeDigits(int length)
        {
            return int.Parse(GenerateRandomDigitsWithTimeStamp(length));
        }
        
        // Generate random digits with specific time stamp
        private static string GenerateRandomDigitsWithTimeStamp(int length)
        {
            var rnd = new Random();
    
            // Get a timestamp (ticks)
            long timestamp = DateTime.Now.Ticks;
    
            // Use the last part of the timestamp to ensure limited size 
            string timestampPart = timestamp.ToString().Substring(timestamp.ToString().Length - Math.Min(8, length));

            // Generate the random digits portion
            string digits = string.Empty;
            for (int i = 0; i < length - timestampPart.Length; ++i)
            {
                digits += rnd.Next(0, 10); 
            }

            // Combine random digits with timestamp part
            return digits + timestampPart;
        }
        
        // Formats a string by replacing placeholders like <0>, <1>, etc., with the provided arguments.
        public static string Format(string input, params string[]? args)
        {
            if (string.IsNullOrEmpty(input))
                return null!;

            if (args == null || args.Length == 0)
                return input; // Return original string if no args provided.

            for (int i = 0; i < args.Length; i++)
            {
                input = input.Replace($"<{i}>", args[i]);
            }

            return input;
        }

        // Add white space to string
        public static string AddWhitespaceToString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Use a regex to identify boundaries between lowercase and uppercase letters
            string result = Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");

            return result;
        }

        // Remove word and add white space to string
        public static string RemoveWordAndAddWhitespace(string input, string wordToRemove)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(wordToRemove))
                return input;

            // Remove the specified word
            string withoutWord = Regex.Replace(input, wordToRemove, "", RegexOptions.IgnoreCase);

            // Add whitespace to the remaining string
            return AddWhitespaceToString(withoutWord);
        }

        // Convert string to CamelCase
        public static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
            {
                return s;
            }

            var chars = s.ToCharArray();

            for (var i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                var hasNext = (i + 1 < chars.Length);
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    break;
                }

                chars[i] = char.ToLower(chars[i], CultureInfo.InvariantCulture);
            }

            return new string(chars);
        }

        //  Summary:
        //      Extract number from specific barcode format such as "SD00001". The result must be "1" as typeof int
        public static int ExtractNumber(string input, string prefix, int length)
        {
            // Define a regex pattern to match the prefix followed by digits of a specific length
            var pattern = $"^{Regex.Escape(prefix)}(\\d{{{length}}})$";
            Match match = Regex.Match(input, pattern);
        
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
        
            return -1; // Return -1 if extraction fail
        }
        
        //  Summary:
        //      Generate a barcode string by completing the number with specific prefix
        public static string AutoCompleteBarcode(string prefix, int length, int number)
        {
            // Format the number to match the required length
            var formattedNumber = number.ToString().PadLeft(length, '0');
            return $"{prefix}{formattedNumber}";
        }
        
        //  Summary:
        //      Generate a barcode string by completing range of number with specific prefix
        public static List<string> AutoCompleteBarcode(string prefix, int length, int min, int max)
        {
            // Initialize list of string 
            var barcodeList = new List<string>();
            for (int num = min; num <= max; num++)
            {
                // Format the number to match the required length
                var formattedNumber = num.ToString().PadLeft(length, '0');
                barcodeList.Add($"{prefix}{formattedNumber}");
            }
            
            return barcodeList;
        }
        
        // Validate numeric & datetime
        public static bool IsDecimal(string text) => decimal.TryParse(text, out _);
        public static bool IsNumeric(string text) => int.TryParse(text, out _);

        public static bool IsDateTime(string text)
        {
            string[] formats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };
            return DateTime.TryParseExact(text, formats, null, DateTimeStyles.None, out _);
        }

        // Validate Http/Https Url
        public static bool IsValidUrl(string url)
        {
            Uri? uriResult;
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        // Validate DCC number
        public static bool IsValidDeweyDecimal(string classificationNumber)
        {
            if (string.IsNullOrWhiteSpace(classificationNumber))
                return false;

            classificationNumber = classificationNumber.Trim();

            var regex = new Regex(@"^\d{1,3}(\.\d{1,10})?$", RegexOptions.Compiled);
            return regex.IsMatch(classificationNumber);
        }

        // Validate Cutter number
        public static bool IsValidCutterNumber(string cutterNumber)
        {
            // Regex for Cutter Numbers
            var regex = new Regex(@"^[A-Z]{1,2}\d{1,4}(\.\d+)?[A-Z]?$");
            return regex.IsMatch(cutterNumber);
        }

        // Validate prefix code 
        public static bool IsValidBarcodeWithPrefix(string barcode, string prefix)
        {
            return Regex.IsMatch(barcode, $@"^{prefix}\d+$");
        }

        // Get Public Id from Url 
        public static string? GetPublicIdFromUrl(string url)
        {
            // Check whether URL is valid
            if (!IsValidUrl(url)) return null;

            var lastSlashIndex = url.LastIndexOf('/');
            var lastDotIndex = url.LastIndexOf('.');

            // Check if both slash and dot exist in the URL
            if (lastSlashIndex == -1 || lastDotIndex == -1 || lastDotIndex < lastSlashIndex) return null;

            // Extract the public ID
            return url.Substring(lastSlashIndex + 1, lastDotIndex - lastSlashIndex - 1);
        }


        public static MatchResultDto CalculateFieldMatchScore(
            string ocrContent,
            List<FieldMatchInputDto> fields,
            double confidenceThreshold,
            int minFieldThreshold)
        {
            var finalContent = RemoveSpecialCharactersOfVietnamese(ocrContent).ToLower().Trim();

            var matchResult = new MatchResultDto
            {
                FieldPointsWithThreshole = new List<FieldMatchedResult>()
            };

            double totalWeightedScore = 0;

            foreach (var field in fields)
            {
                if (!field.Values.Any())
                    continue;

                if (field.FieldName.Equals("Title", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, (int FuzzinessPoint, int MatchPhrasePoint, int MatchedPoint)>
                        titlePoints = new();

                    foreach (var value in field.Values)
                    {
                        var normalizedValue = RemoveSpecialCharactersOfVietnamese(value).ToLower().Trim();

                        // Calculate FuzzinessPoint and MatchPhrasePoint
                        int fuzzinessPoint = FuzzySharp.Fuzz.TokenSetRatio(normalizedValue, finalContent);
                        int matchPhrasePoint = MatchPhraseWithScore(finalContent, normalizedValue);

                        // Calculate MatchedPoint (average of FuzzinessPoint and MatchPhrasePoint)
                        int matchedPoint = (fuzzinessPoint + matchPhrasePoint) / 2;

                        if (!titlePoints.Any() || titlePoints.All(x => x.Value.MatchedPoint <= matchedPoint))
                        {
                            // Keep only the result with the highest MatchedPoint
                            titlePoints.Clear();
                            titlePoints[value] = (fuzzinessPoint, matchPhrasePoint, matchedPoint);
                        }
                    }

                    var bestMatch = titlePoints.First();
                    totalWeightedScore += bestMatch.Value.MatchedPoint * field.Weight;
                    matchResult.FieldPointsWithThreshole.Add(new FieldMatchedResult()
                    {
                        Name = "Title or Subtitle matches most",
                        Detail = bestMatch.Key,
                        FuzzinessPoint = bestMatch.Value.FuzzinessPoint,
                        MatchPhrasePoint = bestMatch.Value.MatchPhrasePoint,
                        MatchedPoint = bestMatch.Value.MatchedPoint,
                        Threshold = minFieldThreshold,
                        IsPassed = bestMatch.Value.MatchedPoint >= minFieldThreshold
                    });
                }
                else if (field.FieldName.Equals("Authors", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, (int FuzzinessPoint, int MatchPhrasePoint, int MatchedPoint)>
                        titlePoints = new();
                    foreach (var value in field.Values)
                    {
                        var normalizedValue = RemoveSpecialCharactersOfVietnamese(value).ToLower().Trim();

                        // Calculate FuzzinessPoint and MatchPhrasePoint
                        int fuzzinessPoint = FuzzySharp.Fuzz.TokenSetRatio(normalizedValue, finalContent);
                        int matchPhrasePoint = MatchPhraseWithScore(finalContent, normalizedValue);

                        // Calculate MatchedPoint (average of FuzzinessPoint and MatchPhrasePoint)
                        int matchedPoint = (fuzzinessPoint + matchPhrasePoint) / 2;

                        if (!titlePoints.Any() || titlePoints.All(x => x.Value.MatchedPoint <= matchedPoint))
                        {
                            // Keep only the result with the highest MatchedPoint
                            titlePoints.Clear();
                            titlePoints[value] = (fuzzinessPoint, matchPhrasePoint, matchedPoint);
                        }
                    }
                    var bestMatch = titlePoints.First();
                    totalWeightedScore += bestMatch.Value.MatchedPoint * field.Weight;
                    matchResult.FieldPointsWithThreshole.Add(new FieldMatchedResult()
                    {
                        Name = "Author",
                        Detail = bestMatch.Key,
                        FuzzinessPoint = bestMatch.Value.FuzzinessPoint,
                        MatchPhrasePoint = bestMatch.Value.MatchPhrasePoint,
                        MatchedPoint = bestMatch.Value.MatchedPoint,
                        Threshold = minFieldThreshold,
                        IsPassed = bestMatch.Value.MatchedPoint >= minFieldThreshold
                    });
                }
                else
                {
                    var normalizedValue = RemoveSpecialCharactersOfVietnamese(field.Values.First()).ToLower().Trim();

                    int fuzzinessPoint = FuzzySharp.Fuzz.TokenSetRatio(normalizedValue, finalContent);
                    int matchPhrasePoint = MatchPhraseWithScore(finalContent, normalizedValue);
                    int matchedPoint = (fuzzinessPoint + matchPhrasePoint) / 2;

                    matchResult.FieldPointsWithThreshole.Add(new FieldMatchedResult()
                    {
                        Name = field.FieldName,
                        Detail = field.Values.First(),
                        FuzzinessPoint = fuzzinessPoint,
                        MatchPhrasePoint = matchPhrasePoint,
                        MatchedPoint = matchedPoint,
                        Threshold = minFieldThreshold,
                        IsPassed = matchedPoint >= minFieldThreshold
                    });

                    totalWeightedScore += matchedPoint * field.Weight;
                }
            }

            matchResult.TotalPoint = totalWeightedScore;
            matchResult.ConfidenceThreshold = confidenceThreshold;
            return matchResult;
        }

        public static int MatchPhraseWithScore(string data, string phrase)
        {
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(phrase))
                return -1;

            // Normalize the input (lowercase and split into words)
            var normalizedData = NormalizeText(data);
            var normalizedPhrase = NormalizeText(phrase);
            var joinedNormalizedPhrase = string.Join(" ", normalizedPhrase);
            var joinedNormalizedData = string.Join(" ", normalizedData);
            // Calculate match score
            double matchScore = CalculateDamerauLevenshteinPercentage( joinedNormalizedData,joinedNormalizedPhrase);

            // Return match score as an integer percentage
            return (int)Math.Round(matchScore);
        }

        /// <summary>
        /// Combine FuzzySharp and Damerau-Levenshtein 
        /// </summary>
        public static int CombinedFuzzinessScore(string data, string phrase)
        {
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(phrase))
                return -1;

            // Normalize input
            var normalizedData = string.Join(" ", NormalizeText(data));
            var normalizedPhrase = string.Join(" ", NormalizeText(phrase));

            // Damerau-Levenshtein score
            double damerauLevenshteinScore = CalculateDamerauLevenshteinPercentage(normalizedData, normalizedPhrase);

            // FuzzySharp score
            double fuzzySharpScore = FuzzySharp.Fuzz.TokenSetRatio(normalizedData, normalizedPhrase);

            // Combine scores using weighted average (adjust weights as needed)
            double combinedScore = (0.6 * damerauLevenshteinScore) + (0.4 * fuzzySharpScore);

            return (int)Math.Round(combinedScore);
        }

        /// <summary>
        /// Damerau-Levenshtein Distance Percentage Calculation
        /// </summary>
        private static double CalculateDamerauLevenshteinPercentage(string source, string target)
        {            
            int distance = DamerauLevenshteinDistance(source, target);
            if (distance == -1)
            {
                return 100;    
            }

            int maxLength = Math.Max(source.Length, target.Length);

            if (maxLength == 0)
                return 100.0;

            return (1.0 - ((double)distance / maxLength)) * 100;
        }

        /// <summary>
        /// Damerau-Levenshtein Distance Calculation
        /// </summary>
        private static int DamerauLevenshteinDistance(string source, string target)
        {
            if (source.Contains(target))
            {
                return -1;
            }

            int m = source.Length;
            int n = target.Length;

            var dp = new int[m + 1, n + 1];

            for (int i = 0; i <= m; i++)
                dp[i, 0] = i;

            for (int j = 0; j <= n; j++)
                dp[0, j] = j;

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), // Deletion/Insertion
                        dp[i - 1, j - 1] + cost // Substitution
                    );

                    if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                    {
                        dp[i, j] = Math.Min(dp[i, j], dp[i - 2, j - 2] + cost); // Transposition
                    }
                }
            }

            return dp[m, n];
        }

        /// <summary>
        /// Normalize input text (lowercase and split into words)
        /// </summary>
        private static List<string> NormalizeText(string text)
        {
            return text.ToLower()
                .Split(new[] { ' ', '.', ',', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        /// <summary>
        /// Calculate the percentage match between a phrase and a data string.
        /// </summary>
        private static double CalculateMatchScoreByMatchPhrase(List<string> phraseWords, List<string> itemWords)
        {
            int phraseLength = phraseWords.Count;
            int itemLength = itemWords.Count;

            if (phraseLength == 0 || itemLength == 0) return 0;

            int maxMatch = 0;

            for (int i = 0; i <= itemLength - phraseLength; i++)
            {
                int currentMatch = 0;

                for (int j = 0; j < phraseLength; j++)
                {
                    if (i + j < itemLength && phraseWords[j] == itemWords[i + j])
                    {
                        currentMatch++;
                    }
                    else
                    {
                        break;
                    }
                }

                maxMatch = Math.Max(maxMatch, currentMatch);
            }

            return (double)maxMatch / phraseLength * 100;
        }


        public static string RemoveSpecialCharactersOfVietnamese(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Chuyển đổi chuỗi thành dạng NFD để tách dấu thanh ra khỏi ký tự gốc
            string normalizedString = content.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark) 
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string RemoveSpecialCharacter(string content)
        {
            return Regex.Replace(content, @"[^0-9a-zA-ZÀ-Ỵà-ỵ\s]", "");
        }

        //get public id in cloudinary url
        public static string GetPublicIdFromCloudinaryUrl(string imageUrl)
        {
            var uri = new Uri(imageUrl);
            var segments = uri.AbsolutePath.Split('/');

            // PublicId is at the end with the extension
            var fileNameWithExtension = segments[^1];
            //remove extension
            var publicId = Path.GetFileNameWithoutExtension(fileNameWithExtension);

            return publicId;
        }

        // Remove special character at the end
        public static string SplitSpecialCharAtTheEnd(string input)
        {
            if (!string.IsNullOrEmpty(input) && char.IsPunctuation(input[^1])) input = input[..^1];
            return input;
        }
    }
}