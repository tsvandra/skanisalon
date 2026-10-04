using System;
using Npgsql;
var connString = "Host=mainline.proxy.rlwy.net;Port=22360;Database=railway;Username=postgres;Password=KwzZJjfTdNiidYPNDPPVDFYGpPClhUmF;TrustServerCertificate=True;";
using var conn = new NpgsqlConnection(connString); conn.Open();
using var cmd = new NpgsqlCommand("SELECT \"Id\", \"StartDateTime\", \"CustomerId\" FROM \"Appointments\" WHERE \"StartDateTime\" >= '2026-09-30' AND \"StartDateTime\" < '2026-10-01'", conn);
using var reader = cmd.ExecuteReader();
while (reader.Read()) Console.WriteLine($"ID: {reader[0]}, Date: {reader[1]}, CustId: {reader[2]}");

