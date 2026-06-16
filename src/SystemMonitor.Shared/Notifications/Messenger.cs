using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace SystemMonitor.Shared.Notifications;

public sealed class Messenger
{
    private const int HeaderSizeBytes = 4;
    private const long MaximumBodyLength = 10 * 1024 * 1024;

    public async Task SendAsync(Stream stream, EventEnvelope eventEnvelope, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(eventEnvelope);
        var body = Encoding.UTF8.GetBytes(json);

        var header = new byte[HeaderSizeBytes];
        BinaryPrimitives.WriteInt32BigEndian(header, body.Length);

        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EventEnvelope?> ReceiveAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderSizeBytes];
        if (!await TryReadAsync(stream, header, cancellationToken).ConfigureAwait(false))
        {
            return default;
        }

        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > MaximumBodyLength)
        {
            return default;
        }

        var body = new byte[length];
        if (!await TryReadAsync(stream, body, cancellationToken).ConfigureAwait(false))
        {
            return default;
        }

        var json = Encoding.UTF8.GetString(body);
        return JsonSerializer.Deserialize<EventEnvelope>(json);
    }

    private async Task<bool> TryReadAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int offset = 0;

        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return false;
            }

            offset += read;
        }

        return true;
    }
}
