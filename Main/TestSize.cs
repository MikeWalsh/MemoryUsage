using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Main
{
    public class TestSize
    {
        /// <summary>
        /// Approximate size of an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        static public int SizeOf<T>(T value)
        {
            return SizeOfObj(typeof(T), value, null);
        }

        /// <summary>
        /// Not entirely clear what we are trying to estimate...
        /// are we using size in memory as an estimate for serialized or the opposite?
        /// If we are trying to get size in memory seems you would need to account for 
        /// a lot more e.g.  https://stackoverflow.com/a/3694530
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        static private int SizeOfObj<T>(Type type, T thevalue, ObjectIDGenerator gen)
        {
            // The general feeling is that this is impossible to do in practise...
            // Issues with memory alignment, boxing, optimizations, registers, structs vs classes, etc

            // Assuming 64bit
            int pointerSizeBytes = 8;

            try
            {
                if (type == null)
                {
                    type = typeof(T);
                }
            }
            catch { }

            if (type == null && thevalue == null)
            {
                // This also seems wrong - should be 4/8 byte pointer?
                // Although I guess pointers aren't counted anywhere else...
                // https://stackoverflow.com/questions/3801878/how-much-memory-does-null-pointer-use
                //return 0;
                return pointerSizeBytes;
            }
            int returnval = 0;
            if (type.IsValueType)
            {
                // Not sure about this e.g. see https://stackoverflow.com/questions/3559843/what-is-the-size-of-a-nullableint32. 
                var nulltype = Nullable.GetUnderlyingType(type);
                // If thevalue is nullable then we need to account for the null type HasValue bool
                // Although this seems to vary depending on the various issues mentioned above
                returnval = (nulltype == null) ? 0 : 4;

                returnval += System.Runtime.InteropServices.Marshal.SizeOf(nulltype ?? type);
            }
            else if (type == typeof(string))
            {
                returnval = pointerSizeBytes + (thevalue == null ? 0 : Encoding.Default.GetByteCount(thevalue as string));
            }
            else if (type.IsArray && type.GetElementType().IsValueType)
            {
                var array = thevalue as Array;
                returnval = pointerSizeBytes
                    + array.GetLength(0) * System.Runtime.InteropServices.Marshal.SizeOf(type.GetElementType());
            }
            else if (thevalue is IEnumerable enumerable)
            {
                // How to take into account the size of the e.g. list/dictionary structure itself?
                // Would it be better to prioritse serialization and only use this as a fallback?

                // A list of non-serializable objects is still marked serializable
                var enumerator = enumerable.GetEnumerator();
                var innerType = GetAnyElementType(type);

                // if the inner type is serializable try to use binary serialization
                if (innerType.IsSerializable)
                {
                    returnval = GetBinarySerializedSize(thevalue);
                }

                // If that didn't work try to recurse
                if (returnval == 0)
                {
                    returnval = pointerSizeBytes;

                    // Otherwise we need to recurse
                    while (enumerator.MoveNext())
                    {
                        var item = enumerator.Current;
                        returnval += SizeOfObj(innerType, item, gen);
                    }
                }
            }
            else if (thevalue is Stream thestream)
            {
                returnval = pointerSizeBytes + (int)thestream.Length;
            }
            else if (type.IsSerializable)
            {
                returnval = GetBinarySerializedSize(thevalue);
            }
            else if (type.IsClass)
            {
                returnval = SizeOfClass(thevalue, gen ?? new ObjectIDGenerator());
            }

            // fallback
            if (returnval == 0)
            {
                try
                {
                    returnval = System.Runtime.InteropServices.Marshal.SizeOf(thevalue);
                }
                catch { }
            }
            return returnval;
        }

        private static int GetBinarySerializedSize<T>(T thevalue)
        {
            int returnval = 0;
            try
            {
                using (Stream s = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(s, thevalue);
                    returnval = (int)s.Length;
                }
            }
            catch { }

            return returnval;
        }

        static Type GetAnyElementType(Type type)
        {
            // Type is Array
            // short-circuit if you expect lots of arrays 
            if (type.IsArray)
                return type.GetElementType();

            // type is IEnumerable<T>;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }


        static private int SizeOfClass(object thevalue, ObjectIDGenerator gen)
        {
            if (thevalue == null)
                return 0;

            gen.GetId(thevalue, out bool isfirstTime);
            if (!isfirstTime) return 0;
            var fields = thevalue.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int returnval = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                object v = fields[i].GetValue(thevalue);
                returnval += 4 + SizeOfObj(fields[i].FieldType, v, gen);
            }
            return returnval;
        }
    }
}
