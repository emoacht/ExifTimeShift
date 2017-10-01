using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExifDateEditor.Models
{
	public static class BytesExtension
	{
		public static byte[] SequenceReplace(this byte[] source, byte[] oldValue, byte[] newValue, int maxCount = -1)
		{
			var sourceIndices = SequenceIndicesOf(source, oldValue, maxCount).ToArray();
			if (!sourceIndices.Any())
				return source;

			Debug.WriteLine("Indices: {0} ({1})", String.Join(", ", sourceIndices), sourceIndices.Length);

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

			int sourceIndex = -1; // -1 is to make it 0 at the first loop.

			foreach (var sourceByte in source)
			{
				sourceIndex++;

				if (sourceByte != valueBytes[valueIndex])
				{
					valueIndex = 0;
					continue;
				}

				if (valueIndex < valueIndexLast)
				{
					valueIndex++;
					continue;
				}

				yield return sourceIndex - valueIndexLast;

				count++;
				if ((0 <= maxCount) && (maxCount <= count))
					yield break;

				valueIndex = 0;
			}
		}

		// Single index by byte[]
		public static int SequenceIndexOf(this byte[] source, byte[] value, int startIndex = 0)
		{
			int valueIndex = 0;
			int valueIndexLast = value.Length - 1;

			for (int sourceIndex = startIndex; sourceIndex < source.Length; sourceIndex++)
			{
				if (source[sourceIndex] != value[valueIndex])
				{
					valueIndex = 0;
					continue;
				}

				if (valueIndex < valueIndexLast)
				{
					valueIndex++;
					continue;
				}

				return sourceIndex - valueIndexLast;
			}

			return -1;
		}

		// Single index by IEnumerable<byte>
		public static int SequenceIndexOf(this IEnumerable<byte> source, IEnumerable<byte> value, int startIndex = 0)
		{
			var valueBytes = value as byte[] ?? value.ToArray();
			int valueIndex = 0;
			int valueIndexLast = valueBytes.Length - 1;

			int sourceIndex = startIndex - 1; // -1 is to make it startIndex at the first loop.

			foreach (var sourceByte in source.Skip(startIndex))
			{
				sourceIndex++;

				if (sourceByte != valueBytes[valueIndex])
				{
					valueIndex = 0;
					continue;
				}

				if (valueIndex < valueIndexLast)
				{
					valueIndex++;
					continue;
				}

				return sourceIndex - valueIndexLast;
			}

			return -1;
		}
	}
}