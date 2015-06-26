namespace ARMAPI_Test
{
    using System.Collections.Generic;

    public class UsagePayload
    {
        public List<UsageAggregate> value { get; set; }

        public string nextLink { get; set; }
    }
}