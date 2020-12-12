using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ExifTimeShift.Models;

namespace ExifTimeShift.Test
{
	[TestClass]
	public class BytesExtensionTest
	{
		// index:                                0              5             10             15             20             25             30             35             40             45             50             55             60             65             70             75             80             85             90             95            100            105            110            115            120            125            130            135            140            145            150            155            160            165            170            175            180            185            190            195            200            205            210            215            220            225            230            235            240            245            250            255            260            265            270            275            280            285            290            295            300            305            310            315            320            325            330            335            340            345            350            355            360            365            370            375            380            385            390            395            400            405            410            415            420            425            430            435            440       
		private const string sourceHexString = "4C 6F 72 65 6D 20 69 70 73 75 6D 20 64 6F 6C 6F 72 20 73 69 74 20 61 6D 65 74 2C 20 63 6F 6E 73 65 63 74 65 74 75 72 20 61 64 69 70 69 73 63 69 6E 67 20 65 6C 69 74 2C 20 73 65 64 20 64 6F 20 65 69 75 73 6D 6F 64 20 74 65 6D 70 6F 72 20 69 6E 63 69 64 69 64 75 6E 74 20 75 74 20 6C 61 62 6F 72 65 20 65 74 20 64 6F 6C 6F 72 65 20 6D 61 67 6E 61 20 61 6C 69 71 75 61 2E 20 55 74 20 65 6E 69 6D 20 61 64 20 6D 69 6E 69 6D 20 76 65 6E 69 61 6D 2C 20 71 75 69 73 20 6E 6F 73 74 72 75 64 20 65 78 65 72 63 69 74 61 74 69 6F 6E 20 75 6C 6C 61 6D 63 6F 20 6C 61 62 6F 72 69 73 20 6E 69 73 69 20 75 74 20 61 6C 69 71 75 69 70 20 65 78 20 65 61 20 63 6F 6D 6D 6F 64 6F 20 63 6F 6E 73 65 71 75 61 74 2E 20 44 75 69 73 20 61 75 74 65 20 69 72 75 72 65 20 64 6F 6C 6F 72 20 69 6E 20 72 65 70 72 65 68 65 6E 64 65 72 69 74 20 69 6E 20 76 6F 6C 75 70 74 61 74 65 20 76 65 6C 69 74 20 65 73 73 65 20 63 69 6C 6C 75 6D 20 64 6F 6C 6F 72 65 20 65 75 20 66 75 67 69 61 74 20 6E 75 6C 6C 61 20 70 61 72 69 61 74 75 72 2E 20 45 78 63 65 70 74 65 75 72 20 73 69 6E 74 20 6F 63 63 61 65 63 61 74 20 63 75 70 69 64 61 74 61 74 20 6E 6F 6E 20 70 72 6F 69 64 65 6E 74 2C 20 73 75 6E 74 20 69 6E 20 63 75 6C 70 61 20 71 75 69 20 6F 66 66 69 63 69 61 20 64 65 73 65 72 75 6E 74 20 6D 6F 6C 6C 69 74 20 61 6E 69 6D 20 69 64 20 65 73 74 20 6C 61 62 6F 72 75 6D";
		private static byte[] _sourceBytes;

		private static byte[] GetBytes(string source) => source.Split().Select(x => Convert.ToByte(x, 16)).ToArray();

		[ClassInitialize]
		public static void BaseClassInitialize(TestContext context)
		{
			_sourceBytes = GetBytes(sourceHexString);
		}

		#region Multiple

		[TestMethod]
		public void SequenceIndicesOfByteArray()
		{
			static bool Execute(string valueHexString, int maxCount, params int[] expected) =>
				BytesExtension.SequenceIndicesOf(_sourceBytes, GetBytes(valueHexString), maxCount)
					.ToArray()
					.SequenceEqual(expected);

			Assert.IsTrue(Execute("73 75", 1, 8));
			Assert.IsTrue(Execute("73 75", 10, 8, 383));

			Assert.IsTrue(Execute("6F 6C", 3, 13, 104, 249));
			Assert.IsTrue(Execute("6F 6C", 10, 13, 104, 249, 275, 303, 419));

			Assert.IsTrue(Execute("4C 6F 72", 10, 0));

			Assert.IsTrue(Execute("74 20 61", 10, 20, 197, 423));

			Assert.IsFalse(BytesExtension.SequenceIndicesOf(_sourceBytes, GetBytes("69 70 74")).Any());
		}

		[TestMethod]
		public void SequenceIndicesOfByteEnumerable()
		{
			static bool Execute(string valueHexString, int maxCount, params int[] expected) =>
				BytesExtension.SequenceIndicesOf(_sourceBytes, GetBytes(valueHexString).AsEnumerable(), maxCount)
					.ToArray()
					.SequenceEqual(expected);

			Assert.IsTrue(Execute("73 75", 1, 8));
			Assert.IsTrue(Execute("73 75", 10, 8, 383));

			Assert.IsTrue(Execute("6F 6C", 3, 13, 104, 249));
			Assert.IsTrue(Execute("6F 6C", 10, 13, 104, 249, 275, 303, 419));

			Assert.IsTrue(Execute("4C 6F 72", 10, 0));

			Assert.IsTrue(Execute("74 20 61", 10, 20, 197, 423));

			Assert.IsFalse(BytesExtension.SequenceIndicesOf(_sourceBytes, GetBytes("69 70 74").AsEnumerable()).Any());
		}

		#endregion

		#region Single

		[TestMethod]
		public void SequenceIndexOfByteArray()
		{
			static int Execute(string valueHexString, int startIndex) =>
				BytesExtension.SequenceIndexOf(_sourceBytes, GetBytes(valueHexString), startIndex);

			Assert.AreEqual(1, Execute("6F", 0));

			Assert.AreEqual(8, Execute("73 75", 0));
			Assert.AreEqual(383, Execute("73 75", 10));
			Assert.AreEqual(-1, Execute("73 75", 390));

			Assert.AreEqual(13, Execute("6F 6C", 0));
			Assert.AreEqual(104, Execute("6F 6C", 20));
			Assert.AreEqual(249, Execute("6F 6C", 110));
			Assert.AreEqual(275, Execute("6F 6C", 250));
			Assert.AreEqual(303, Execute("6F 6C", 280));
			Assert.AreEqual(419, Execute("6F 6C", 310));
			Assert.AreEqual(-1, Execute("6F 6C", 420));

			Assert.AreEqual(0, Execute("4C 6F 72", 0));
			Assert.AreEqual(-1, Execute("4C 6F 72", 10));

			Assert.AreEqual(20, Execute("74 20 61", 0));
			Assert.AreEqual(197, Execute("74 20 61", 30));
			Assert.AreEqual(423, Execute("74 20 61", 200));

			Assert.AreEqual(46, Execute("63 69 6E 67", 0));

			Assert.AreEqual(243, Execute("72 75 72 65 20", 0));

			Assert.AreEqual(438, Execute("61 62 6F 72 75 6D", 0));
		}

		[TestMethod]
		public void SequenceIndexOfByteEnumerable()
		{
			static int Execute(string valueHexString, int startIndex) =>
				BytesExtension.SequenceIndexOf(_sourceBytes, GetBytes(valueHexString).AsEnumerable(), startIndex);

			Assert.AreEqual(1, Execute("6F", 0));

			Assert.AreEqual(8, Execute("73 75", 0));
			Assert.AreEqual(383, Execute("73 75", 10));
			Assert.AreEqual(-1, Execute("73 75", 390));

			Assert.AreEqual(13, Execute("6F 6C", 0));
			Assert.AreEqual(104, Execute("6F 6C", 20));
			Assert.AreEqual(249, Execute("6F 6C", 110));
			Assert.AreEqual(275, Execute("6F 6C", 250));
			Assert.AreEqual(303, Execute("6F 6C", 280));
			Assert.AreEqual(419, Execute("6F 6C", 310));
			Assert.AreEqual(-1, Execute("6F 6C", 420));

			Assert.AreEqual(0, Execute("4C 6F 72", 0));
			Assert.AreEqual(-1, Execute("4C 6F 72", 10));

			Assert.AreEqual(20, Execute("74 20 61", 0));
			Assert.AreEqual(197, Execute("74 20 61", 30));
			Assert.AreEqual(423, Execute("74 20 61", 200));

			Assert.AreEqual(46, Execute("63 69 6E 67", 0));

			Assert.AreEqual(243, Execute("72 75 72 65 20", 0));

			Assert.AreEqual(438, Execute("61 62 6F 72 75 6D", 0));
		}

		#endregion
	}
}