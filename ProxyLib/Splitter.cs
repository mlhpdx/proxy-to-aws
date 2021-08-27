using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Listeners
{
	public interface IStreamSplitter
	{
		Task<byte[]> GetNextSegment(Stream s);
	}

	public class SplitterFactory
	{
		static Dictionary<string, Func<string[], IStreamSplitter>> _constructors = new Dictionary<string, Func<string[], IStreamSplitter>>() {
			{ "mqtt", args => new MqttSpliter() },
			{ "radius", args => new RadiusSplitter() },
			{ "ipfix", args => new VariableLengthSplitter(VariableLengthSplitter.LengthValueType.UInt16, 2, true) },
			{ "fixed", args => new FixedLengthSplitter(int.Parse(args[0])) },
			{ "variable", args => new VariableLengthSplitter((VariableLengthSplitter.LengthValueType)int.Parse(args[0]), int.Parse(args[1]), bool.Parse(args[2])) },
			{ "suffix", args => new FourByteSuffixSplitter(uint.Parse(args[0])) },
			{ "full",  args => new NoopSpliiter() }
		};

		public static IStreamSplitter GetSplitter(string spec) {
			var parts = spec.Split(new[] { ':' }, 2);
			var name = parts.FirstOrDefault();
			var args = parts.Skip(1).FirstOrDefault()?.Split(new[] { ',' });
			return _constructors[name?.ToLower()](args);
		}
	}

	abstract class BaseSplitter : IStreamSplitter
	{
		protected static async Task<int> FillBuffer(Stream s, byte[] b, int o = 0) {
			int offset = o, read;
			do {
				offset += read = await s.ReadAsync(b, offset, b.Length - offset);
			} while (read > 0 && offset < b.Length);
			return offset;
		}
		public abstract Task<byte[]> GetNextSegment(Stream s);
	}

	class FixedLengthSplitter : BaseSplitter
	{
		public FixedLengthSplitter(long length) {
			SegmentLength = length;
		}
		public long SegmentLength { get; private set; }
		public override async Task<byte[]> GetNextSegment(Stream s) {
			var segment = new byte[SegmentLength];
			var length = await FillBuffer(s, segment);
			Array.Resize(ref segment, length);
			return segment;
		}
	}

	class MqttSpliter : BaseSplitter
	{
		public MqttSpliter() { }
		public override async Task<byte[]> GetNextSegment(Stream s) {
			var header = new byte[5] { (byte)s.ReadByte(), 0, 0, 0, 0 };
			var bytes = 0;
			var multiplier = 1;
			var length = 0;
			do {
				bytes++;
				header[bytes] = (byte)s.ReadByte();
				length += multiplier * (header[bytes] & 127);
				multiplier *= 128;
			} while (bytes < 5 && (header[bytes] & 128) != 0);

			var buffer = new byte[1 + bytes + length];
			Array.Copy(header, buffer, 1 + bytes);
			var read = await FillBuffer(s, buffer, 1 + bytes);
			if (read != length)
				throw new InvalidDataException("MQTT Message Length") { Data = { { "ExpectedLength", length }, { "LengthRead", read } } };
			return buffer;
		}
	}

	class VariableLengthSplitter : BaseSplitter
	{
		public enum LengthValueType { Byte = 1, UInt16 = 2, UInt32 = 4 }
		public VariableLengthSplitter(LengthValueType lengthValueSize, int lengthValueOffset, bool reverseBytes) {
			ValueOffset = lengthValueOffset;
			ValueSize = lengthValueSize;
			ReverseBytes = reverseBytes;
		}
		public int ValueOffset { get; private set; }
		public LengthValueType ValueSize { get; private set; }
		public bool ReverseBytes { get; private set; }
		private int GetLengthValue(byte[] bytes) {
			switch (ValueSize) {
				case LengthValueType.Byte:
					return bytes[0];
				case LengthValueType.UInt16:
					return BitConverter.ToInt16(bytes, 0);
				case LengthValueType.UInt32:
					return BitConverter.ToInt32(bytes, 0);
				default:
					throw new NotImplementedException("Unsupported Value Size");
			}
		}
		public override async Task<byte[]> GetNextSegment(Stream s) {
			var header = new byte[ValueOffset + (int)ValueSize];
			if (await FillBuffer(s, header) < header.Length)
				throw new InvalidOperationException();

			var values = header.Skip(ValueOffset);
			if (ReverseBytes) values = values.Reverse();
			var length = GetLengthValue(values.ToArray());
			var payload = new byte[length];

			header.CopyTo(payload, 0);
			await FillBuffer(s, payload, header.Length);

			return payload;
		}
	}

	class FourByteSuffixSplitter : BaseSplitter {
		public FourByteSuffixSplitter(uint magic) {
			Magic = magic;
		}
		public uint Magic { get; private set; }
		public override async Task<byte[]> GetNextSegment(Stream s) {
			uint view = 0;
			using (var ms = new MemoryStream()) {
				while (true) {
					var b = s.ReadByte();
					if (b == -1)
						return ms.ToArray(); // suffix not found, return null?

					ms.WriteByte((byte)b);
					view = (view << 8) | (byte)b;
					if (view == Magic) {
						return await Task.FromResult(ms.ToArray());
					}
				}
			}
		}
	}

	class RadiusSplitter : VariableLengthSplitter
	{
		public RadiusSplitter() : base(LengthValueType.UInt16, 2, true) { }
	}

	class NoopSpliiter : BaseSplitter
	{
		override public async Task<byte[]> GetNextSegment(Stream s) {
			using (var ms = new MemoryStream()) {
				await s.CopyToAsync(ms);
				return ms.ToArray();
			}
		}
	}
}
