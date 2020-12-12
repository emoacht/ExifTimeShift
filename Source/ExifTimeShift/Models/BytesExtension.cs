using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExifTimeShift.Models
{
	public static class BytesExtension
	{
		public static byte[] SequenceReplace(this byte[] source, byte[] oldValue, byte[] newValue, int maxCount = -1)
		{
			var sourceIndices = SequenceIndicesOf(source, oldValue, maxCount).ToArray();
			if (!sourceIndices.Any())
				return source;

			var destination = new byte[source.Length + (newValue.Length - oldValue.Length) * sourceIndices.Length];

			// Copy source before old value.
			var sourceIndexFirst = sourceIndices.First();
			if (0 < sourceIndexFirst)
			{
				Buffer.BlockCopy(source, 0, destination, 0, sourceIndexFirst);
			}

			for (int i = 0; i < sourceIndices.Length; i++)
			{
				var sourceIndex = sourceIndices[i];
				var destinationIndex = sourceIndex + (newValue.Length - oldValue.Length) * i;

				// Copy new value.
				Buffer.BlockCopy(newValue, 0, destination, destinationIndex, newValue.Length);

				// Copy source after new value before next old value.
				var sourceOffset = sourceIndex + oldValue.Length;
				var sourceOffsetNext = (i < sourceIndices.Length - 1) ? sourceIndices[i + 1] : source.Length;

				Buffer.BlockCopy(
				  source,
				  sourceOffset,
				  destination,
				  destinationIndex + newValue.Length,
				  sourceOffsetNext - sourceOffset);
			}

			return destination;
		}

		// Multiple indices by byte[]
		public static IEnumerable<int> SequenceIndicesOf(this byte[] source, byte[] value, int maxCount = -1)
		{
			int count = 0;
			int startIndex = 0;

			while (true)
			{
				var index = source.SequenceIndexOf(value, startIndex);
				if (index < 0)
					yield break;

				yield return index;

				count++;
				if ((0 <= maxCount) && (maxCount <= count))
					yield break;

				startIndex = index + value.Length;
			}
		}

		// Multiple indices by IEnumerable<byte>
		public static IEnumerable<int> SequenceIndicesOf(this IEnumerable<byte> source, IEnumerable<byte> value, int maxCount = -1)
		{
			int count = 0;

			var valueBytes = value as byte[] ?? value.ToArray();
			int valueIndex = 0;

			int valueIndexLast = valueBytes.Length - 1;
			if (valueIndexLast < 0)
				throw new ArgumentException(nameof(value));

			int sourceIndex = 0;

			foreach (var sourceByte in source)
			{
				if (sourceByte == valueBytes[valueIndex])
				{
					if (valueIndex == valueIndexLast)
					{
						yield return sourceIndex - valueIndexLast; // Found

						count++;
						if ((0 <= maxCount) && (maxCount <= count))
							yield break;

						valueIndex = 0;
					}
					else
					{
						valueIndex++;
					}
				}
				else
				{
					valueIndex = 0;
				}

				sourceIndex++;
			}
		}

		// Single index by byte[]
		public static int SequenceIndexOf(this byte[] source, byte[] value, int startIndex = 0)
		{
			int valueIndex = 0;

			int valueIndexLast = value.Length - 1;
			if (valueIndexLast < 0)
				throw new ArgumentException(nameof(value));

			for (int sourceIndex = startIndex; sourceIndex < source.Length; sourceIndex++)
			{
				if (source[sourceIndex] == value[valueIndex])
				{
					if (valueIndex == valueIndexLast)
					{
						return sourceIndex - valueIndexLast; // Found
					}
					else
					{
						valueIndex++;
					}
				}
				else
				{
					valueIndex = 0;
				}
			}

			return -1;
		}

		// Single index by IEnumerable<byte>
		public static int SequenceIndexOf(this IEnumerable<byte> source, IEnumerable<byte> value, int startIndex = 0)
		{
			var valueBytes = value as byte[] ?? value.ToArray();
			int valueIndex = 0;

			int valueIndexLast = valueBytes.Length - 1;
			if (valueIndexLast < 0)
				throw new ArgumentException(nameof(value));

			int sourceIndex = startIndex;

			foreach (var sourceByte in source.Skip(startIndex))
			{
				if (sourceByte == valueBytes[valueIndex])
				{
					if (valueIndex == valueIndexLast)
					{
						return sourceIndex - valueIndexLast; // Found
					}
					else
					{
						valueIndex++;
					}
				}
				else
				{
					valueIndex = 0;
				}

				sourceIndex++;
			}

			return -1;
		}

		public static void Compare(byte[] a, byte[] b)
		{
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] == b[i])
					continue;

				Debug.WriteLine("Position: {0} (0x{0:X4}) Value: {1} -> {2}",
				  i,
				  BitConverter.ToString(new[] { a[i] }),
				  BitConverter.ToString(new[] { b[i] }));
			}
		}
	}
}