using System.Diagnostics;
using OpenTelemetry;

namespace Nop.Web.Framework.Infrastructure;

public class HttpPathSpanProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        if (data.Kind != ActivityKind.Server)
            return;

        var method = data.GetTagItem("http.request.method") as string;
        var path = data.GetTagItem("url.path") as string;

        if (method != null && path != null)
            data.DisplayName = $"{method} {path}";

        base.OnEnd(data);
    }
}
