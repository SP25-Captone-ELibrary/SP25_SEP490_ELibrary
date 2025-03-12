namespace FPTU_ELibrary.API.Payloads.Requests.CustomVision;

public class ExtendTrainingProgressRequest
{
    public List<ItemsAndImagesForTraining> ListItemsAndImagesForTraining { get; set; }
}

public class ItemsAndImagesForTraining
{
    public List<int> ItemIds { get; set; }
    public List<string> ImageUrls { get; set; }
}

    public static class ExtendTrainingProgressRequestExtension
    {
        public static (IDictionary<int, List<int>>, IDictionary<int, List<string>>) ToTrainingData(
            this ExtendTrainingProgressRequest req)
        {
            var itemIds = new Dictionary<int, List<int>>();
            var images = new Dictionary<int, List<string>>();
            for (var i = 0; i < req.ListItemsAndImagesForTraining.Count; i++)
            {
                itemIds.Add(i, req.ListItemsAndImagesForTraining[i].ItemIds);
                images.Add(i, req.ListItemsAndImagesForTraining[i].ImageUrls);
            }
            return (itemIds, images);
        }
    }