﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OTransport.NetworkChannel.TCP;
using OTransport;
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
    public class TCPNetworkChannel_Server
    {
        TCPServerChannel tcpserver = new TCPServerChannel();
        TCPClientChannel tcpclient = new TCPClientChannel();
        TCPClientChannel tcpclient2 = new TCPClientChannel();

        [TestCleanup]
        public void CleanUpServer()
        {
            if (tcpserver != null)
                tcpserver.Stop();
            if (tcpclient != null)
                tcpclient.Stop();
            if (tcpclient2 != null)
                tcpclient2.Stop();

            TCPObjectTransportChannel.TearDown();
        }
        [TestMethod]
        public void TCPServer_WhenClientConnects_CallbackFunctionCalled()
        {
            bool connected = false;
            tcpserver.Start("127.0.0.1", 0);
            tcpserver.OnClientConnect(c => connected = true);

            tcpclient.Start("127.0.0.1", tcpserver.LocalPort);

            Utilities.WaitFor(ref connected);
            Assert.IsTrue(connected);
        }
        [TestMethod]
        public void TCPServer_ReceivesObjects_CorrectObjectReceived()
        {
            //Arrange
            MockObjectMessage receivedObject = null;
            var connectTransports = TCPObjectTransportChannel.GetConnectObjectTransports();
            var server = connectTransports.Item1;
            var client = connectTransports.Item2;

            //Act
            server.Receive<MockObjectMessage>(o =>
            {
                receivedObject = o;

            }).Execute();

            client.Send(new MockObjectMessage()
            {
                Property1_string = "hello world!",
                Property2_int = 123,
                Property3_decimal = 12.3M
            }).Execute();

            Utilities.WaitFor(ref receivedObject);
            //Assert
            Assert.AreEqual("hello world!", receivedObject.Property1_string);
            Assert.AreEqual(123, receivedObject.Property2_int);
            Assert.AreEqual(12.3M, receivedObject.Property3_decimal);
        }

        [TestMethod]
        public void TCPServer_SendObject_CorrectObjectSent()
        {
            //Arrange
            MockObjectMessage receivedObject = null;
            tcpserver.Start("127.0.0.1", 0);

            Client client = null;

            ObjectTransport serverObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpserver);
            serverObjectTransport.OnClientConnect(c => client = c);

            tcpclient.Start("127.0.0.1", tcpserver.LocalPort);
            ObjectTransport clientObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpclient);

            Utilities.WaitFor(ref client);

            //Act

            clientObjectTransport.Receive<MockObjectMessage>(o =>
            receivedObject = o
            ).Execute();

            serverObjectTransport.Send(new MockObjectMessage()
            {
                Property1_string = "hello world!",
                Property2_int = 123,
                Property3_decimal = 12.3M

            })
            .To(client)
            .Execute();

            Utilities.WaitFor(ref receivedObject);
            //Assert
            Assert.AreEqual("hello world!", receivedObject.Property1_string);
            Assert.AreEqual(123, receivedObject.Property2_int);
            Assert.AreEqual(12.3M, receivedObject.Property3_decimal);
        }

        [TestMethod]
        public void TCPServer_ClientDisconnects_CallbackCalled()
        {
            //Arrange
            Client clientConnect = null;
            Client clientDisconnect = null;

            tcpserver.Start("127.0.0.1", 0);
            ObjectTransport serverObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpserver);
            serverObjectTransport.OnClientConnect(c => clientConnect = c);
            serverObjectTransport.OnClientDisconnect(c => clientDisconnect = c);

            tcpclient.Start("127.0.0.1", tcpserver.LocalPort);
            ObjectTransport clientObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpclient);

            Utilities.WaitFor(ref clientConnect);
            Utilities.WaitFor(() => clientObjectTransport.GetConnectedClients().Count() ==1);
            //Act

            clientObjectTransport.Stop();

            Utilities.WaitFor(ref clientDisconnect);
            Utilities.Wait();
            //Assert
            Assert.AreEqual(clientConnect.IPAddress, "127.0.0.1");
            Assert.AreEqual(clientDisconnect.IPAddress, "127.0.0.1");
            Assert.AreEqual(clientDisconnect,clientConnect);
            Assert.AreEqual(0,clientObjectTransport.GetConnectedClients().Count());
            Assert.AreEqual(0,serverObjectTransport.GetConnectedClients().Count());
        }

        [TestMethod]
        public void TCPServerWith2Clients_Disconnect1Client_1ClientDisconnected()
        {
            //Arrange
            Client disconnectedClient = null;

            tcpserver.Start("127.0.0.1", 0);
            ObjectTransport serverObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpserver);
            serverObjectTransport.OnClientDisconnect(c => disconnectedClient = c);

            tcpclient.Start("127.0.0.1", tcpserver.LocalPort);
            ObjectTransport clientObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpclient);

            tcpclient2 = new TCPClientChannel();
            tcpclient2.Start("127.0.0.1", tcpserver.LocalPort);
            ObjectTransport clientObjectTransport2 = TestObjectTransportFactory.CreateNewObjectTransport(tcpclient2);

            Utilities.WaitFor(() => serverObjectTransport.GetConnectedClients().Count() == 2);

            //Act

            var FirstClient = serverObjectTransport.GetConnectedClients().First();
            serverObjectTransport.DisconnectClient(FirstClient);

            Utilities.WaitFor(ref disconnectedClient);

            //Assert
            Client LastClient = serverObjectTransport.GetConnectedClients().First();

            Assert.AreEqual(1, serverObjectTransport.GetConnectedClients().Count());
            Assert.AreNotEqual(FirstClient.Port, LastClient.Port);
        }

        
        [TestMethod]
        public void TCPServerWith2Clients_Disconnect2Client_AllClientsDisconnected()
        {
            //Arrange
            List<Client> disconnectedClients = new List<Client>();

            tcpserver.Start("127.0.0.1", 0);
            ObjectTransport serverObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpserver);
            serverObjectTransport.OnClientDisconnect(c => disconnectedClients.Add(c));

            tcpclient.Start("127.0.0.1", tcpserver.LocalPort);
            ObjectTransport clientObjectTransport = TestObjectTransportFactory.CreateNewObjectTransport(tcpclient);

            tcpclient2 = new TCPClientChannel();
            tcpclient2.Start("127.0.0.1", tcpserver.LocalPort);
            ObjectTransport clientObjectTransport2 = TestObjectTransportFactory.CreateNewObjectTransport(tcpclient2);

            Utilities.WaitFor(() => serverObjectTransport.GetConnectedClients().Count() == 2);

            //Act

            var allClients = serverObjectTransport.GetConnectedClients().ToArray();
            serverObjectTransport.DisconnectClient(allClients);

            Utilities.WaitFor(() => disconnectedClients.Count == 2);

            //Assert
            Assert.AreEqual(0, serverObjectTransport.GetConnectedClients().Count());
            Assert.AreEqual(2, disconnectedClients.Count());
        }
        


    }
}
