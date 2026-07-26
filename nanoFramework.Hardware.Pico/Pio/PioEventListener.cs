//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Runtime.Events;
using System;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Listens for PIO events and dispatches them to the appropriate <see cref="PioBlock"/> instances.
    /// </summary>
    internal class PioEventListener : IEventProcessor, IEventListener
    {
        private readonly PioBlock[] _pioMap = new PioBlock[3];
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="PioEventListener"/> class and registers it with the event sink.
        /// </summary>
        public PioEventListener()
        {
            EventSink.AddEventProcessor(EventCategory.PicoPio, this);
            EventSink.AddEventListener(EventCategory.PicoPio, this);
        }

        /// <summary>
        /// Processes the event data and creates a <see cref="PioEvent"/> instance.
        /// </summary>
        /// <param name="data1">The first data parameter.</param>
        /// <param name="data2">The second data parameter.</param>
        /// <param name="time">The time of the event.</param>
        /// <returns>The processed event.</returns>
        public BaseEvent ProcessEvent(uint data1, uint data2, DateTime time)
        {
            return new PioEvent
            {
                BlockIndex = (int)(data1 >> 16),
                Flags = (PioInterruptFlags)data2
            };
        }

        /// <summary>
        /// Initializes the event listener for the event source.
        /// </summary>
        public void InitializeForEventSource()
        {
        }

        /// <summary>
        /// Handles the event by dispatching it to the appropriate <see cref="PioBlock"/> based on the block index.
        /// </summary>
        /// <param name="ev">The event to handle.</param>
        /// <returns>The confirmation of event handling.</returns>
        public bool OnEvent(BaseEvent ev)
        {
            PioBlock block = null;
            PioEvent pioEvent = (PioEvent)ev;

            lock (_syncRoot)
            {
                if (pioEvent.BlockIndex >= 0 && pioEvent.BlockIndex < _pioMap.Length)
                {
                    block = _pioMap[pioEvent.BlockIndex];
                }
            }

            if (block != null)
            {
                block.OnInterruptInternal(pioEvent.Flags);
            }

            return true;
        }

        /// <summary>
        /// Adds a <see cref="PioBlock"/> to the event listener for dispatching events.
        /// </summary>
        /// <param name="block">The <see cref="PioBlock"/> to add.</param>
        public void AddBlock(PioBlock block)
        {
            lock (_syncRoot)
            {
                if (block.Index >= 0 && block.Index < _pioMap.Length)
                {
                    _pioMap[block.Index] = block;
                }
            }
        }

        /// <summary>
        /// Removes a <see cref="PioBlock"/> from the event listener.
        /// </summary>
        /// <param name="blockIndex">The index of the <see cref="PioBlock"/> to remove.</param>
        public void RemoveBlock(int blockIndex)
        {
            lock (_syncRoot)
            {
                if (blockIndex >= 0 && blockIndex < _pioMap.Length)
                {
                    _pioMap[blockIndex] = null;
                }
            }
        }
    }
}