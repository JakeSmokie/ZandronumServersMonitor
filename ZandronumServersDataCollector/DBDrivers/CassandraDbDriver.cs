﻿using System;
using System.Collections.Generic;
using System.Net;
using Cassandra;
using ZandronumServersDataCollector.ServerDataFetchers;

namespace ZandronumServersDataCollector.DBDrivers {
    public class CassandraDbDriver : IDbDriver {
        private Cluster _cluster;
        private ISession _session;
        private PreparedStatement _insertPreparedStatement;

        public void Connect() {
            _cluster = Cluster.Builder()
                .AddContactPoints("127.0.0.1")
                .Build();

            _session = _cluster.Connect("sdf");
            _insertPreparedStatement =
                _session.Prepare(
                    "INSERT INTO servers (logged_at, ip, port, name, ping, version) VALUES (?, ?, ?, ?, ?, ?);");
        }

        public void InsertServerData(ServerData serverData) {
            var statement = _insertPreparedStatement
                .Bind(
                    serverData.LogTime,
                    serverData.Address.Address,
                    (short) serverData.Address.Port,
                    serverData.Name,
                    serverData.Ping,
                    serverData.Version);

            _session.Execute(statement);
        }

        public IEnumerable<ServerData> SelectServerData() {
            var rs = _session.Execute("SELECT * FROM servers");

            foreach (var server in rs) {
                var serverData = new ServerData {
                    LogTime = server.GetValue<DateTime>("logged_at"),
                    Address = new IPEndPoint(
                        server.GetValue<IPAddress>("ip"),
                        (ushort) server.GetValue<short>("port")),
                    Name = server.GetValue<string>("name"),
                    Ping = server.GetValue<short>("ping"),
                    Version = server.GetValue<string>("version")
                };

                yield return serverData;
            }
        }
    }
}