namespace AzureMonitor.Models
{
    public class ResourceListResult
    {
        public string nextLink { get; set; }
        public GenericResourceExpanded[] value { get; set; }
    }


}