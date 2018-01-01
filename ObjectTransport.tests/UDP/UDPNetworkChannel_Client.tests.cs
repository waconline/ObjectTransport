﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OTransport;
using OTransport.NetworkChannel.UDP;
using OTransport.Test.Utilities;
using OTransport.tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Test
{
    [TestClass]
    public class UDPNetworkChannel_Client
    {
        UDPServerChannel server = null;
        UDPClientChannel udpclient = null;

        [TestCleanup]
        public void CleanUpServer()
        {
            if (server != null)
                server.Stop();
            if (udpclient != null)
                udpclient.Stop();
        }
        [TestMethod]
        public void UDPClient_ClientDisconnects_CallbackCalled()
        {
            //Arrange
            Client client = null;
            Client clientDisconnect = null;

            server = new UDPServerChannel("127.0.0.1", 0,32);
            ObjectTransport serverObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(server);

            udpclient = new UDPClientChannel("127.0.0.1", server.Port);
            ObjectTransport clientObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(udpclient);

            clientObjectTransport.OnClientDisconnect(c => clientDisconnect = c);
            client = clientObjectTransport.GetConnectedClients().First();

            Utilities.WaitFor(ref client);
            Utilities.WaitFor(()=> serverObjectTransport.GetConnectedClients().Count() == 1);
            //Act

            serverObjectTransport.Stop();

            Utilities.WaitFor(ref clientDisconnect);
            //Assert
            Assert.AreEqual(client.IPAddress, "127.0.0.1");
            Assert.AreEqual(clientDisconnect.IPAddress, "127.0.0.1");
            Assert.AreEqual(clientDisconnect,client);
            Utilities.WaitFor(()=>clientObjectTransport.GetConnectedClients().Count() == 0);
            Utilities.WaitFor(()=>serverObjectTransport.GetConnectedClients().Count() == 0);
        }
    }
}