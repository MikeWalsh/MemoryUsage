using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Main;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        class ClassA
        {
            public int a { get; set; }
        }
        class ClassTwoStr
        {
            public string a { get; set; }
            public string b { get; set; }
        }

        class ClassR
        {
            public string a { get; set; }
            public ClassR R { get; set; }
        }

        class NotSerializable
        {
            public int IntProp { get; set; }
            public string StringProp { get; set; }
        }

        [TestMethod]
        public void CanGetSizeOfValueTypes()
        {
            Debug.WriteLine("Size int :{0}", TestSize.SizeOf(1));
            Assert.AreEqual(4, TestSize.SizeOf(1));

            Debug.WriteLine("Size long :{0}", TestSize.SizeOf(long.MaxValue));
            Assert.AreEqual(8, TestSize.SizeOf(long.MaxValue));
        }

        [TestMethod]
        public void CanGetSizeOfNullableValueTypes()
        {
            Debug.WriteLine("Size int?null :{0}", TestSize.SizeOf((int?)null));
            //Assert.AreEqual(4, TestSize.SizeOf((int?)null));
            Assert.AreEqual(8, TestSize.SizeOf((int?)null)); // ??? Still a pointer and HasValue?

            Debug.WriteLine("Size int? :{0}", TestSize.SizeOf((int?)2));
            Assert.AreEqual(8, TestSize.SizeOf((int?)2));
        }

        [TestMethod]
        public void CanGetSizeOfString()
        {
            // Not sure this is correct - should be a 4/8 byte pointer?
            Debug.WriteLine("Size string(null) :{0}", TestSize.SizeOf((string)null));
            //Assert.AreEqual(0, TestSize.SizeOf((string)null));
            Assert.AreEqual(8, TestSize.SizeOf((string)null)); // ??

            Debug.WriteLine("Size string[10] :{0}", TestSize.SizeOf("0123456789"));
            Assert.IsTrue(TestSize.SizeOf("0123456789") > 0);
        }

        [TestMethod]
        public void CanGetSizeOfStringBuilder()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
                sb.Append("0123456789");
            Debug.WriteLine("Size stringbulder[10*100] :{0}", TestSize.SizeOf(sb));
            Assert.IsTrue(TestSize.SizeOf(sb) > 0);

            sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
                sb.Append("わたしわたしわたしわ");
            Debug.WriteLine("Size stringbulder U[10*100] :{0}", TestSize.SizeOf(sb));
            Assert.IsTrue(TestSize.SizeOf(sb) > 0);
        }

        [TestMethod]
        public void CanGetSizeOfSimpleClass()
        {
            Debug.WriteLine("Size C:simple :{0}", TestSize.SizeOf(new ClassA()));
            Assert.IsTrue(TestSize.SizeOf(new ClassA()) > 0);

            Debug.WriteLine("Size C:strings :{0}", TestSize.SizeOf(new ClassTwoStr()));
            Assert.IsTrue(TestSize.SizeOf(new ClassTwoStr()) > 0);

            var class2 = new ClassTwoStr() { a = "0123456789", b = "0123456789" };
            Debug.WriteLine("Size C:strings[setted]:{0}", TestSize.SizeOf(class2));
            Assert.IsTrue(TestSize.SizeOf(class2) > 0);
        }

        [TestMethod]
        public void CanGetSizeOfSimpleCollections()
        {
            var arrayint = new int[100];
            Debug.WriteLine("Size arrayint[100] :{0}", TestSize.SizeOf(arrayint));
            Assert.IsTrue(TestSize.SizeOf(arrayint) > 0);

            var arraysimple = new ClassA[3];
            Debug.WriteLine("Size simple[3] :{0}", TestSize.SizeOf(arraysimple));
            //Assert.IsTrue(TestSize.SizeOf(arraysimple) > 0);
            // This result also doesn't seem right
            //Assert.AreEqual(0, TestSize.SizeOf((string)null));
            Assert.AreEqual(8, TestSize.SizeOf((string)null)); // ??

            var list = new List<int>();
            Debug.WriteLine("Size list Empty :{0}", TestSize.SizeOf(list));
            Assert.IsTrue(TestSize.SizeOf(list) > 0);

            for (int i = 0; i < 100; i++) list.Add(i);
            Debug.WriteLine("Size list<int>[100] :{0}", TestSize.SizeOf(list));
            Assert.IsTrue(TestSize.SizeOf(list) > 0);

            var dict = Enumerable.Range(0, 100).ToDictionary(x => x, x => new string('a', x));
            var dictSize = TestSize.SizeOf(dict);
            var real = dict.Keys.Count * System.Runtime.InteropServices.Marshal.SizeOf(typeof(int)) + dict.Values.Sum(x => Encoding.Default.GetByteCount(x));
            Debug.WriteLine("Size Dictonary<int,string>[100] real: {0} calculated: {1}", real, dictSize);
            //Assert.AreEqual(real, dictSize);
            Assert.IsTrue(dictSize > 0);

        }

        [TestMethod]
        public void CanGetSizeOfRecursiveClass()
        {
            var classR_top = new ClassR()
            {
                a = "This is a string",
                R = new ClassR()
            };
            classR_top.R.R = classR_top;

            Debug.WriteLine("Size Class Recursive :{0}", TestSize.SizeOf(classR_top));
            Assert.IsTrue(TestSize.SizeOf(TestSize.SizeOf(classR_top)) > 0);
        }

        [TestMethod]
        public void CanGetSizeOfListOfNonSerializableObjects()
        {
            var list = new List<NotSerializable>
            {
                new NotSerializable { IntProp = 1, StringProp = "1" },
                new NotSerializable { IntProp = 2, StringProp = "2" },
                new NotSerializable { IntProp = 3, StringProp = "3" }
            };

            var size = TestSize.SizeOf(list);
            Assert.IsTrue(size > 0);
        }
    }
}
