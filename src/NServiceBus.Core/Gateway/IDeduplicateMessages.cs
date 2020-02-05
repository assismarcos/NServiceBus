﻿namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Defines the api for storages that wants to provide storage for gateway deduplication.
    /// </summary>
    [ObsoleteEx(
            Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
    public interface IDeduplicateMessages
    {
        /// <summary>
        /// Returns false if the message is a duplicate.
        /// </summary>
        /// <param name="clientId">The client id that defines the range of ids to check for duplicates.</param>
        /// <param name="timeReceived">The time received of the message to allow the storage to do cleanup.</param>
        /// <param name="context">The current pipeline context.</param>
        Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context);
    }
}