//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Runtime.Events;
using System;

namespace nanoFramework.Hardware.Pico.Pio
{
    internal class PioEventListener : IEventProcessor, IEventListener
    {
        private const byte PioEventMessage = 100;
        private readonly PioBlock[] _pioMap = new PioBlock[3];
        private readonly object _syncRoot = new object();

        public PioEventListener()
        {
            EventSink.AddEventProcessor(EventCategory.PicoPio, this);
            EventSink.AddEventListener(EventCategory.PicoPio, this);
        }

        public BaseEvent ProcessEvent(uint data1, uint data2, DateTime time)
        {
            return new PioEvent
            {
                BlockIndex = (int)(data1 >> 16),
                Flags = data2
            };
        }

        public void InitializeForEventSource()
        {
        }

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