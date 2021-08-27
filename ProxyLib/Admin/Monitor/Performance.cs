namespace ProxyLib.Admin.Monitor
{
	class Performance : BaseMonitorMessage<Performance>
	{
		public int ListenerCount;
		public int DestinationCount;

		public int ActiveDestinations;
		public int BackloggedDestinations;

		public int TotalBacklogLength;

		public double MsgsReceivedRate;
		public double MsgsSentRate;

		public long IncommingPacketRate;
		public long IncommingPacketDropRate;
	}
}
