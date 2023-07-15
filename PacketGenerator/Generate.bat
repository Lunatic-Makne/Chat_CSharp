@echo off

start /wait PacketGenerator.exe.lnk Packet

xcopy /Y /V .\\Packet.cs ..\\Protocol\\Packet.cs

start /wait PacketGenerator.exe.lnk ServerHandler

xcopy /Y /V .\\ServerPacketHandler.cs ..\\DummyClient\\PacketHandler\\ServerPacketHandler.cs

start /wait PacketGenerator.exe.lnk ClientHandler

xcopy /Y /V .\\ClientPacketHandler.cs ..\\ChatServer\\PacketHandler\\ClientPacketHandler.cs
