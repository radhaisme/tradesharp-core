/***************************************************************************** 
* Copyright 2016 Aurora Solutions 
* 
*    http://www.aurorasolutions.io 
* 
* Aurora Solutions is an innovative services and product company at 
* the forefront of the software industry, with processes and practices 
* involving Domain Driven Design(DDD), Agile methodologies to build 
* scalable, secure, reliable and high performance products.
* 
* TradeSharp is a C# based data feed and broker neutral Algorithmic 
* Trading Platform that lets trading firms or individuals automate 
* any rules based trading strategies in stocks, forex and ETFs. 
* TradeSharp allows users to connect to providers like Tradier Brokerage, 
* IQFeed, FXCM, Blackwood, Forexware, Integral, HotSpot, Currenex, 
* Interactive Brokers and more. 
* Key features: Place and Manage Orders, Risk Management, 
* Generate Customized Reports etc 
* 
* Licensed under the Apache License, Version 2.0 (the "License"); 
* you may not use this file except in compliance with the License. 
* You may obtain a copy of the License at 
* 
*    http://www.apache.org/licenses/LICENSE-2.0 
* 
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, 
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
* See the License for the specific language governing permissions and 
* limitations under the License. 
*****************************************************************************/


﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using NUnit.Framework;
using TradeHub.Common.Core.Constants;
using TradeHub.Common.Core.DomainModels;
using TradeHub.Common.Core.FactoryMethods;
using TradeHub.Common.Core.ValueObjects.MarketData;
using TradeHub.StrategyRunner.Infrastructure.ValueObjects;
using TradeHub.StrategyRunner.MarketDataController.Service;

namespace TradeHub.StrategyRunner.MarketDataController.Tests.Integration
{
    [TestFixture]
    class MarketDataTestCases : IEventHandler<MarketDataObject>
    {
        private DataHandler _dataHandler;

        private bool _barArrived = false;
        private bool _tickArrived = false;
        
        private ManualResetEvent _barArrivedEvent;
        private ManualResetEvent _tickArrivedEvent;

        [SetUp]
        public void StartUp()
        {
        }

        [TearDown]
        public void CloseDown()
        {
            
        }

        [Test]
        [Category("Integration")]
        public void LiveBarsMarketDataTestCase()
        {
            _dataHandler = new DataHandler();

            bool barArrived = false;
            ManualResetEvent barArrivedEvent = new ManualResetEvent(false);
            // Get new Security object
            Security security = new Security { Symbol = "ERX" };

            // Get Bar subscription message
            BarDataRequest barSubscribeRequest = SubscriptionMessage.LiveBarSubscription("001", security, BarFormat.TIME,
                                                                                         BarPriceType.LAST, 60, 0.0001M,
                                                                                         0, "SimulatedExchange");

            _dataHandler.BarReceived += delegate(Bar obj)
                {
                    barArrived = true;
                    barArrivedEvent.Set();
                };

            _dataHandler.SubscribeSymbol(barSubscribeRequest);

            barArrivedEvent.WaitOne(2000);

            Assert.IsTrue(barArrived);
        }

        [Test]
        [Category("Integration")]
        public void TicksMarketDataTestCase()
        {
            _dataHandler = new DataHandler();

            bool tickArrived = false;
            ManualResetEvent tickArrivedEvent = new ManualResetEvent(false);

            // Get new Security object
            Security security = new Security { Symbol = "ERX" };

            // Get Tick subscription message
            var subscribe = SubscriptionMessage.TickSubscription("001", security, "SimulatedExchange");

            _dataHandler.TickReceived += delegate(Tick obj)
            {
                tickArrived = true;
                tickArrivedEvent.Set();
            };

            _dataHandler.SubscribeSymbol(subscribe);

            tickArrivedEvent.WaitOne(2000);

            Assert.IsTrue(tickArrived);
        }

        [Test]
        [Category("Integration")]
        public void LiveBarsInLocalDisruptorMarketDataTestCase()
        {
            _dataHandler = new DataHandler(new IEventHandler<MarketDataObject>[] {this});

            _barArrivedEvent = new ManualResetEvent(false);
            // Get new Security object
            Security security = new Security { Symbol = "ERX" };

            // Get Bar subscription message
            BarDataRequest barSubscribeRequest = SubscriptionMessage.LiveBarSubscription("001", security, BarFormat.TIME,
                                                                                         BarPriceType.LAST, 60, 0.0001M,
                                                                                         0, "SimulatedExchange");

            _dataHandler.SubscribeSymbol(barSubscribeRequest);

            _barArrivedEvent.WaitOne(2000);

            Assert.IsTrue(_barArrived);
        }

        [Test]
        [Category("Integration")]
        public void TicksInLocalDisruptorMarketDataTestCase()
        {
            _dataHandler = new DataHandler(new IEventHandler<MarketDataObject>[] { this });

            _tickArrivedEvent = new ManualResetEvent(false);

            // Get new Security object
            Security security = new Security { Symbol = "ERX" };

            // Get Tick subscription message
            var subscribe = SubscriptionMessage.TickSubscription("001", security, "SimulatedExchange");

            _dataHandler.SubscribeSymbol(subscribe);

            _tickArrivedEvent.WaitOne(2000);

            Assert.IsTrue(_tickArrived);
        }

        /// <summary>
        /// Called when new Tick is received
        /// </summary>
        /// <param name="tick"></param>
        private void OnTickArrived(Tick tick)
        {
            _tickArrived = true;
            _tickArrivedEvent.Set();
        }

        /// <summary>
        /// Called when new Bar is received
        /// </summary>
        /// <param name="bar"></param>
        private void OnBarArrived(Bar bar)
        {
            _barArrived = true;
            _barArrivedEvent.Set();
        }

        #region Implementation of IEventHandler<in MarketDataObject>

        /// <summary>
        /// Called when a publisher has committed an event to the <see cref="T:Disruptor.RingBuffer`1"/>
        /// </summary>
        /// <param name="data">Data committed to the <see cref="T:Disruptor.RingBuffer`1"/></param><param name="sequence">Sequence number committed to the <see cref="T:Disruptor.RingBuffer`1"/></param><param name="endOfBatch">flag to indicate if this is the last event in a batch from the <see cref="T:Disruptor.RingBuffer`1"/></param>
        public void OnNext(MarketDataObject data, long sequence, bool endOfBatch)
        {
            if(data.IsTick)
                OnTickArrived(data.Tick);
            else
                OnBarArrived(data.Bar);
        }

        #endregion
    }
}
