using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infra.Repositories.Documents;

public sealed class EventDocument
{
    public Guid Id { get; set; }              // EventId
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public DateTime OccurredOn { get; set; }
    public long Version { get; set; }

    public object Data { get; set; } = default!;
}
