using System.Runtime.Serialization;

namespace Gateway.Admin.Controllers
{
    [DataContract]
    public class RouteInfo
    {
        [DataMember]
        public string PathMatcher { get; set; }
        [DataMember]
        public bool IsPartitioned { get; set; }
        [DataMember]
        public string ServiceUri { get; set; }
        [DataMember]
        public string ListenerName { get; set; }
        [DataMember]
        public bool IsOpen { get; set; }

        [DataMember]
        public string CorrelationId { get; set; }

        public override string ToString()
        {
            return $"CorrelationId: {CorrelationId}, Match: {PathMatcher}, Partitioned: {IsPartitioned}, Open: {IsOpen}, Listener: {ListenerName}, Service: {ServiceUri}";
        }
    }
}