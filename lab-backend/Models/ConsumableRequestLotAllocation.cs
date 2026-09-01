namespace LabManagementAPI.Models;

public class ConsumableRequestLotAllocation
{
    public long Id { get; set; }
    public int ConsumableRequestId { get; set; }
    public ConsumableRequest? ConsumableRequest { get; set; }
    public int ConsumableLotId { get; set; }
    public ConsumableLot? ConsumableLot { get; set; }
    public int Quantity { get; set; }
}
