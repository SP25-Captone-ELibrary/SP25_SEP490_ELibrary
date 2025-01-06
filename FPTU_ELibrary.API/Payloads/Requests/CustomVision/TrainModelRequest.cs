using FPTU_ELibrary.Application.Dtos.AIServices.Classification;

namespace FPTU_ELibrary.API.Payloads.Requests.CustomVision;

public class BaseTrainedModelRequest
{
    public List<IFormFile> ImageList { get; set; }
}

public class TrainModelAfterCreateRequest : BaseTrainedModelRequest
{
    public Guid BookCode { get; set; }
}   