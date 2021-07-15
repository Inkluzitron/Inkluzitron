using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface IMessageEventHandler
    {
        Task<bool> HandleMessageUpdatedAsync(IMessageChannel channel, IMessage updatedMessage, Lazy<Task<IMessage>> freshMessageFactory)
            => Task.FromResult(false);

        Task<bool> HandleMessageDeletedAsync(IMessageChannel channel, ulong messageId)
            => Task.FromResult(false);

        Task<bool> HandleMessagesBulkDeletedAsync(IMessageChannel channel, IReadOnlyCollection<ulong> messageIds)
            => Task.FromResult(false);
    }
}
