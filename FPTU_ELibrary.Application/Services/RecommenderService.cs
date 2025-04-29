using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Diacritics.Extensions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Recommendation;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using StopWord;

namespace FPTU_ELibrary.Application.Services;

public class RecommenderService : IRecommenderService
{
    private readonly IUserService<UserDto> _userSvc;
    private readonly ILibraryItemService<LibraryItemDto> _libItemSvc;
    private readonly ILibraryCardService<LibraryCardDto> _libCardSvc;

    private readonly ILogger _logger;
    private readonly ICacheService _cacheSvc;
    private readonly ISystemMessageService _msgService;
    
    private readonly AppSettings _appSettings;

    public RecommenderService(
        ILogger logger,
        ICacheService cacheSvc,
        ISystemMessageService msgService,
        IUserService<UserDto> userSvc,
        ILibraryItemService<LibraryItemDto> libItemSvc,
        ILibraryCardService<LibraryCardDto> libCardSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _logger = logger;
        _userSvc = userSvc;
        _cacheSvc = cacheSvc;
        _msgService = msgService;
        _libItemSvc = libItemSvc;
        _libCardSvc = libCardSvc;
        _appSettings = monitor.CurrentValue;
    }
    
    public async Task<IServiceResult> GetRecommendedItemAsync(string email, RecommendFilterDto filter)
    {
        try
        {
            // Check exist user
            // Build spec
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Retrieve user with spec
            var userDto = (await _userSvc.GetWithSpecAsync(userSpec)).Data as UserDto;
            if (userDto == null)
            {
                // Retrieve items with high borrow/reserve rates
                return await _libItemSvc.GetHighBorrowOrReserveRateItemsAsync(filter.PageIndex, filter.PageSize);
            }
            
            // Retrieve all user activities
            var userActivities = (await _libCardSvc.GetAllUserActivityAsync(userDto.UserId)).Data as List<UserProfileActivity>;
            if (userActivities == null || !userActivities.Any())
            {
                // Retrieve items with high borrow/reserve rates
                return await _libItemSvc.GetHighBorrowOrReserveRateItemsAsync(filter.PageIndex, filter.PageSize);
            }
            
            // Retrieve all items for recommendation
            // var libItems = (await _cacheSvc.GetOrAddLibraryItemForRecommendationAsync()).Data as List<LibraryItemDto>;
            var libItems = (await _libItemSvc.GetAllForRecommendationAsync()).Data as List<LibraryItemDto>;
            if (libItems == null || !libItems.Any())
            {
                // Data not found or empty
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0004,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), 
                    data: new List<LibraryItemDto>());
            }
            
            // Build items' profile vector
            var itemVectors = (await BuildVocabularyAndItemVectorsAsync(
                libItems: libItems,
                filter: filter)).ToList();
            // Build the user's profile vector
            var userProfileVector = await BuildUserProfileAsync(
                itemVectors: itemVectors, 
                userActivities: userActivities);
            
            // Initialize collection of item's recommendation scoring 
            var scores = new List<(int LibraryItemId, double Score)>();
            
            // Initialize consumed item ids 
            var consumedItemIds = new List<int>();
            // Initialize interacted item
            var interactedItemIds = new List<int>();
            // Iterate each user activity to retrieve consumed library item id
            userActivities.ForEach(u =>
            {
                // Check whether with specific user has consumed with or not (borrow, reserve)
                if (u.Borrowed || u.Reserved)
                {
                    // Add to consumed item list (to extract all consumed item, only recommend for new items)
                    consumedItemIds.Add(u.LibraryItemId);
                }

                // Has interacted with item (no matter what's action)
                if (u.Borrowed || u.Reserved || u.Rating > 0 || u.Favorite)
                {
                    // Add to interacted item list
                    interactedItemIds.Add(u.LibraryItemId);
                }
            });
            
            // Retrieve user activities' ddc
            var activityClassificationNums = (await _libItemSvc.GetItemClassificationNumAsync(interactedItemIds.ToArray())
                ).Data as List<string?>;
            // Extract all item vectors' ddc
            var itemVectorIds = itemVectors.Select(li => li.LibraryItemId).ToArray();
            // Retrieve item vectors' ddc
            var vectorClassificationNums = (await _libItemSvc.GetItemClassificationNumAsync(itemVectorIds)
                ).Data as List<GetVectorClassificationNumResult>;
            
            // Calculate cosine similarity between each item's TF-IDF vector and user profile
            foreach (var itemVector in itemVectors)
            {
                // Perform calculate cosine similarity
                double similarity = CosineSimilarity(vecA: itemVector.TfidfVector, vecB: userProfileVector);
                
                // Exclude all items that not have any similarity with user's preference
                if (similarity > 0)
                {
                    var libraryItemId = itemVector.LibraryItemId;
                    
                    // Only process update similarity threshold when exist at least one ddc in user activities
                    if (activityClassificationNums != null &&
                        activityClassificationNums.Any())
                    {
                        // Retrieve item' ddc
                        var itemClassificationNum = vectorClassificationNums?.FirstOrDefault(v => 
                            v.LibraryItemId == libraryItemId);
                        // Reduce similarity level by 50% when item's ddc not exist in activities' ddc range
                        var isMatch = DeweyDecimalUtils.IsDDCWithinRange(
                            ddc: itemClassificationNum?.ClassificationNumber,
                            ddcList: activityClassificationNums.ToArray());
                        if(!isMatch) similarity *= 0.5;
                    }
                    
                    // Add key-pair value
                    scores.Add((libraryItemId, similarity));
                }
            }
            
            // Order the items by descending similarity and return the top N
            var recommendedIds = scores.OrderByDescending(s => s.Score)
                .Where(s => !interactedItemIds.Contains(s.LibraryItemId)) // Exclude all interacted items
                .Select(s => s.LibraryItemId)
                .ToList();
            
            // Initialize recommended item collection
            var recommendedItems = new List<LibraryItemDto>();
            // Iterate each recommended id to retrieve item's detail
            foreach (var itemId in recommendedIds)
            {
                var itemDto = libItems.FirstOrDefault(li => li.LibraryItemId == itemId);
                if(itemDto != null) recommendedItems.Add(itemDto);
            }
            // Group all items to add take limits for each item based on author's name
            var groupedByAuthorIds = recommendedItems
                .GroupBy(l => l.LibraryItemAuthors.FirstOrDefault()?.Author.FullName ?? "No Author")
                // Select all items in group. Process apply limit when request
                .SelectMany(g => filter.LimitWorksOfAuthor ? g.Take(5) : g.ToList())
                .Select(l => l.LibraryItemId)
                .ToList();
            
            // Take N items only and exclude all items not existing in grouped
            recommendedItems = recommendedItems.Where(l => groupedByAuthorIds.Contains(l.LibraryItemId)).ToList();
            
            // Pagination
            int pageIndex = filter.PageIndex ?? 1;
            int pageSize = filter.PageSize ?? _appSettings.PageSize;
            // Count total library items
            var totalItem = recommendedItems.Count;
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalItem / pageSize);
            // Set pagination to specification after count total library item
            if (pageIndex > totalPage || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            // Apply pagination
            recommendedItems = recommendedItems.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            
            // Pagination result 
            var paginationResultDto = new PaginatedResultDto<LibraryItemDto>(recommendedItems,
                pageIndex, pageSize, totalPage, totalItem);
            
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: paginationResultDto);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invokes when get recommended item");
        }
    }

    /// <summary>
    /// Calculate cosine similarity between items' TF-IDF vector and user profile's vector
    /// </summary>
    private double CosineSimilarity(Dictionary<string, double> vecA, Dictionary<string, double> vecB)
    {
        // [Norm definition](https://machinelearningcoban.com/math/#-norms-chuan)
        
        // Step-by-step example of how the function works
        // The example would replace the TF-IDF value of each term by term's appearance times
        // 1. vecA represents item A (e.g., Harry Potter và phòng chứa bí mật)
        // 2. vecB represents all user liked items (e.g, Harry Potter và hòn đá phù thủy, Harry Potter và bảo bối tử thần, etc.)
        
        /* Assumptions
            var vecA = new Dictionary<string, double>
            {
                { "harry", 3 },    // "harry" appears 3 times
                { "potter", 2 },   // "potter" appears 2 times
                { "bi", 1 },       // "bi" appears 1 times
                { "mat", 1 },      // "mat" appears 1 times
            };   
            
            var vecB = new Dictionary<string, double>
            {
                { "harry", 2 },    // "harry" appears 2 times
                { "potter", 3 },   // "potter" appears 3 times
                { "phu", 1 },      // "phu" appears 1 times
                { "thuy", 1 },     // "thuy" appears 1 times
                { "tu", 1 },       // "tu" appears 1 times
                { "than", 1 },     // "than" appears 1 times
            };
        */ 
        
        /* Calculation
            Definition:
            + Dot Product (tích vô hướng): được sử dụng để đo lường mức độ tương đồng giữa 
            hai tài liệu (ở đây là các thông tin về sách như tiêu đề, phụ đề, tóm tắt, v.v.) 
            dựa trên tần suất xuất hiện của các từ.
            => Nếu dot product có giá trị cao, cho thấy các tài liệu có nhiều từ chung với tần suất cao,
            từ đó cho thấy mức độ tương đồng giữa chúng cũng cao.
            
            + Norm: giúp điều chỉnh (chuẩn hóa) các vector về một độ dài tiêu chuẩn
            => Nếu không chuẩn hóa, một vector có giá trị lớn hơn chỉ vì nó có tổng các giá trị cao hơn sẽ 
            cho kết quả không chính xác về mức độ tương đồng so với vector kia.
            
            1. Dot Product Calculation
                + For key "harry":
                    > vecA["harry"] = 3 and vecB has "harry" with a value of 2
                    > Total: 3 x 2 = 6
                + For key "potter": 
                    > vecA["potter"] = 2 and vecA has "potter" with a value of 3
                    > Total: 2 x 3 = 6 
                + For key "bi": 
                    > vecA["bi"] = 1 and vecB does not contain "bi"
                    > Total: 1 x 0 = 0
                + For key "mat": 
                    > vecA["mat"] = 1 and vecB does not contain "mat"
                    > Total: 1 x 0 = 0
            => dotProduct = 6 + 6 + 0 + 0 = 12
            
            2. Norm Calculation
                + vecA:
                    normA = sqrt(3^2 + 2^2 + (1^2 x 2)) ~ 3.87
                + vecB:
                    normB = sqrt(2^2 + 3^2 + (1^2 x 4)) ~ 4.12
                    
            3. Cosine Similarity
                CS = dotProduct / (normA x normB)
                CS = 12 / (3.87 x 4.12) ~ 0.75
            
            => 0.75 indicating that Item A (Harry Potter và phòng chứa bí mật) should be recommended for user
         */
        
        // Initialize fields
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;
        
        // Calculate dot product and norm for vecA
        foreach (var key in vecA.Keys)
        {
            dotProduct += vecA[key] * (vecB.ContainsKey(key) ? vecB[key] : 0);
            normA += vecA[key] * vecA[key];
        }
        // Calculate norm for vecB
        foreach (var val in vecB.Values)
        {
            normB += val * val;
        }
        normA = Math.Sqrt(normA);
        normB = Math.Sqrt(normB);
        
        // Return 0 if either vector has zero
        if (normA == 0 || normB == 0) return 0;
        return dotProduct / (normA * normB);
    }
    
    /// <summary>
    /// Build vocabulary and compute TF-IDF vectors for all library items
    /// </summary>
    private async Task<IEnumerable<ItemProfileVector>> BuildVocabularyAndItemVectorsAsync(
        RecommendFilterDto filter,
        List<LibraryItemDto> libItems)
    {
        try
        {
            // Return empty when not found any existing item
            if(libItems.Count == 0) return [];
            
            // Initialize collection of item vector to store profile (feature vector) for each item
            List<ItemProfileVector> itemVectors = new();
            // Initialize of Term Frequency dictionaries for each item (TF)
            List<Dictionary<string, int>> itemTermFrequencies = new();
            // Initialize global vocabulary: term -> index
            Dictionary<string, int> termIndex = new();
            // Initialize document frequency: term -> number of documents that contain the term
            Dictionary<string, double> documentFrequency = new();
            
            // Iterate each item to calculate Term Frequency (TF) 
            foreach (var item in libItems)
            {
                // Aggregated item's attributes to generate recommend text-based 
                var recommendTextBase = await BuildUpRecommendTextAsync(item, filter);
                
                // Only process adding Term Frequency (TF) when existing combined text
                if (!string.IsNullOrEmpty(recommendTextBase))
                {
                    // Tokenize the text (split pure text into array of words and eliminate stop words or perform stemming)
                    var tokens = await TokenizeAsync(recommendTextBase);
                
                    // Build frequency dictionary for each item
                    var frequency = new Dictionary<string, int>();
                    foreach (var token in tokens)
                    {
                        // Try adding tokenized string to dictionary 
                        // Add default as 1 when not exist
                        if (!frequency.TryAdd(token, 1))
                            frequency[token]++; // Increase a number of word count
                    }
                    // Add frequency for specific string collection
                    // E.g., "The Harry Potter harry"
                    // |:Word|: Total count|
                    // | The |      1      | 
                    // | Harry |    2      |
                    // | Potter |   1      | 
                    itemTermFrequencies.Add(frequency);
                
                    // Update global vocabs (distinct token)
                    foreach (var token in tokens.Distinct())
                    {
                        if(!termIndex.ContainsKey(token))
                            termIndex[token] = termIndex.Count;
                    }
                } 
            }
            
            // Calculate Document Frequency (DF) for each term in the vocabulary
            foreach (var term in termIndex.Keys)
            {
                // Count number of documents that contain specific term
                int docCount = itemTermFrequencies.Count(freq => freq.ContainsKey(term));
                // Assign to dic (not require to check exist as term ensure to be unique)
                documentFrequency[term] = docCount;
            }
            
            // Count total docs (total items)
            var totalDocs = libItems.Count;
            // Calculate the TF-IDF vector for each item
            for (int i = 0; i < totalDocs; i++)
            {
                var item = libItems[i]; // Retrieve item at specific index
                var frequency = itemTermFrequencies[i]; // Retrieve frequency at specific index (same as item)
                
                // Create item vector for current library item
                var vector = new ItemProfileVector() { LibraryItemId = item.LibraryItemId };
                
                // Calculate the sum of term frequencies
                var sumFrequencies = frequency.Values.Sum();
                
                // Compute TF-IDF weight for each item 
                foreach (var term in frequency.Keys)
                {
                    double tf = (double)frequency[term] / sumFrequencies;
                    double idf = Math.Log((double)totalDocs / (documentFrequency[term] + 1)); // Add 1 to DF to avoid division by zero
                    double tfidf = tf * idf;
                    vector.TfidfVector[term] = tfidf;
                }
                itemVectors.Add(vector);
            }
            
            return itemVectors;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invokes when process build vocabulary and item vectors");
        }
    }

    /// <summary>
    /// Build up recommend text based on input recommend filter (apply includes: title, author, genres, topical terms)
    /// </summary>
    private async Task<string> BuildUpRecommendTextAsync(LibraryItemDto item, RecommendFilterDto filter)
    {
        // Remove all digits and special characters from Title
        var cleanedTitle = Regex.Replace(item.Title, @"\d", "");

        // Remove all numbers after the dot in the classification number
        var ddc = item.ClassificationNumber ?? string.Empty;
        int dotIndex = ddc.IndexOf('.');
        if (dotIndex >= 0)
        {
            ddc = ddc.Substring(0, dotIndex);
        }
        
        // Initialize author name
        var authorName = string.Empty;
        // Retrieve author name by item id 
        var libItemAuthorDto = (await _libItemSvc.GetFirstAuthorAsync(id: item.LibraryItemId)).Data as LibraryItemAuthorDto;
        if(libItemAuthorDto != null && libItemAuthorDto.Author != null!) authorName = libItemAuthorDto.Author.FullName;
        
        // Initialize combined text
        var combinedText = string.Empty;
        // Determine item's category to customize combined text
        switch (item.Category.EnglishName)
        {
            case nameof(LibraryItemCategory.SingleBook) or
                 nameof(LibraryItemCategory.BookSeries) or
                 nameof(LibraryItemCategory.ReferenceBook):
                // Allow includes: title, ddc, cutter number, author name, genres, topical terms
                // Apply title to recommend text (if any)
                if(filter.IncludeTitle) combinedText += $"{cleanedTitle} "; // Cleaned title
                // Apply author information to recommend text (if any)
                if(filter.IncludeAuthor) combinedText += $"{item.CutterNumber} {authorName} "; // Cutter number + author name
                // Apply genres information to recommend text (if any)
                if (filter.IncludeGenres) combinedText += $"{ddc} {item.Genres} "; // DDC + genres
                // Apply topical terms information to recommend text
                if (filter.IncludeTopicalTerms) combinedText += $"{item.TopicalTerms} ";
                break;
            case nameof(LibraryItemCategory.Newspaper) or
                 nameof(LibraryItemCategory.Magazine):
                // Allow includes: title
                // Apply title to recommend text (if any)
                if(filter.IncludeTitle) combinedText += $"{cleanedTitle}"; // Cleaned title
                break;
        }

        // Force applying if combined text is empty
        if (string.IsNullOrEmpty(combinedText))
        {
            if (!string.IsNullOrEmpty(authorName)) // Default as apply author name (if exist)
            {
                combinedText += $"{authorName}";
            }
            else // Apply title
            {
                combinedText += $"{cleanedTitle}";
            }
        }

        return combinedText;
    }
    
    /// <summary>
    /// Build user profile vector by calculating TF-IDF vectors of liked items
    /// </summary>
    private async Task<Dictionary<string, double>> BuildUserProfileAsync(
        List<ItemProfileVector> itemVectors,
        List<UserProfileActivity> userActivities)
    {
        try
        {
            // Initialize user profile
            var userProfile = new Dictionary<string, double>();
            double totalWeight = 0;
            
            // Aggregate each item's vector using the rating as weight
            foreach (var userAct in userActivities)
            {
                // Retrieve item vectors
                var itemVector = itemVectors.FirstOrDefault(x => x.LibraryItemId == userAct.LibraryItemId);
                if(itemVector == null) continue;
                
                // Calculate user's weight for specific item
                var weight = CalculateActivityWeight(userAct);
                // Add to total weight
                totalWeight += weight;

                // Iterate item's TF-IDF vector to generate user profile vector
                foreach (var kvp in itemVector.TfidfVector)
                {
                    // E.g, If user has only rated "Harry Potter" with 4 stars and "Dac Nhan Tam" with 3 stars
                    // |:  Key   |:  Weight | 
                    // | harry   |    1.6   |
                    // | potter  |    1.6   |
                    // | dac     |    0.8   |
                    // | nhan    |    0.8   |
                    // | tam     |    0.8   |
                    
                    if (userProfile.ContainsKey(kvp.Key))
                        // Append with existing term
                        userProfile[kvp.Key] += kvp.Value * weight;
                    else
                        // "harry" = TF-IDF val * weight
                        userProfile[kvp.Key] = kvp.Value * weight;
                }
            }

            if (totalWeight > 0)
            {
                // For each term (key) in user profile
                foreach (var key in userProfile.Keys.ToList())
                {
                    // Divide the accumulated weight for that term by total weight
                    // Convert sum value to average val for each item
                    userProfile[key] /= totalWeight;
                }
            }
            
            return await Task.FromResult(userProfile);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invokes when process build user profile");
        }
    }

    /// <summary>
    /// Calculate weight based on borrowing, reserving, favorite activities and rating action
    /// </summary>
    private double CalculateActivityWeight(UserProfileActivity activity)
    {
        // Initialize weight val
        double weight = 0.0;

        // Reduce the weight 20% if not consumed
        double consumptionPenalty = 0.8; 
        
        // Only process rating if valid positive rating exists (rating > 2)
        if (activity.Rating > 2)
        {
            // Default rating weight
            // Rating 3 -> 1, 4 -> 2, 5 -> 3.
            double ratingWeight = activity.Rating - 2;
            
            // If user has not borrowed item
            // Apply the consumption penalty
            if (!activity.Borrowed && activity.BorrowCount > 0)
            {
                ratingWeight *= consumptionPenalty;
            }
            
            // Add rate weight
            weight += ratingWeight;
        }
        else // Has not rated item yet
        {
            if (activity.Borrowed && activity.BorrowCount > 0)
            {
                weight += 1.5;
            }
            if (activity.Reserved && activity.ReserveCount > 0)
            {
                weight += 1.0;
            }
        }
        
        // Ignore rating or consumption, favorite action indicating strong interest
        if (activity.Favorite)
        {
            weight += 2.0;
        }
    
        return weight;
    }
    
    /// <summary>
    /// Retrieve Vietnamese stop words from specific route
    /// </summary>
    private async Task<HashSet<string>> RetrieveVietnameseStopWordsAsync()
    {
        try
        {
            var vieStopWords = StopWords.GetStopWords(CultureInfo.GetCultureInfo("vi-VN"));
            return await Task.FromResult(new HashSet<string>(vieStopWords));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invokes when process retrieve vietnamese stop words");
        } 
    }

    /// <summary>
    /// Retrieve English stop words from specific route
    /// </summary>
    private async Task<HashSet<string>> RetrieveEnglishStopWordsAsync()
    {
        try
        {
            var englishStopWords = StopWords.GetStopWords(CultureInfo.GetCultureInfo("en-US"));
            return await Task.FromResult(new HashSet<string>(englishStopWords));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invokes when process retrieve english stop words");
        }
    }
    
    /// <summary>
    /// Tokenize input text into an array of string
    /// Combined with remove stop words to improve the reliable of data response
    /// </summary>
    private async Task<List<string>> TokenizeAsync(string text)
    {
        try
        {
            // Return default string collection when input text is null or empty
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            
            // Normalize text to lowercase
            text = text.ToLower();
            
            // Initialize special characters for splitting text
            char[] delimiters = new char[] 
            { 
                ' ', '\r', '\n', ',', '.', ';', ':', '-', '_', '!', '?', '(', ')', '[', ']', '{', '}', '"' 
            };
            var tokens = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            // Retrieve Vietnamese stop word list
            var vieStopWords = await RetrieveVietnameseStopWordsAsync();
            // Retrieve English stop word list
            var engStopWords = await RetrieveEnglishStopWordsAsync();
            // Combined hashset
            var combinedStopWords = new HashSet<string>(vieStopWords);
            // All elements that are present in the specified collection or both
            combinedStopWords.UnionWith(engStopWords);
            
            // Filter all stop words
            return tokens.Where(token => !combinedStopWords.Contains(token))
                .Select(str => str.RemoveDiacritics()) // Remove vietnamese diacritics (e.g. 'á' -> 'a')
                .ToList(); // Convert to list
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invokes when process tokenize");
        }
    }
}