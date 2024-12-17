using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Supercell.Laser.Logic.UHelper;

public static class Helper
{
    public static void Skip()
    {
        ;
    }

    public static string GetIpFromDomain(string domain)
    {
        return domain[..domain.IndexOf(':')];
    }

    public static string RandomString(int length)
    {
        var result = new char[length];
        for (var i = 0; i < result.Length; i++)
            result[i] = Table.StringCharacters[new Random().Next(Table.StringCharacters.Length)];
        return new string(result);
    }

    public static string ConvertStringToUnderscore(string input)
    {
        var charArray = input.ToCharArray();
        for (var i = 0; i < charArray.Length; i++)
            if (charArray[i] == '_' && i < charArray.Length - 1)
            {
                charArray[i] = char.ToUpper(charArray[i + 1]);
                {
                    Array.Copy(charArray, i + 2, charArray, i + 1, charArray.Length - i - 2);
                    Array.Resize(ref charArray, charArray.Length - 1);
                }
            }
            else if (i == 0)
            {
                charArray[i] = char.ToLower(charArray[i]);
            }

        return new string(charArray);
    }

    public static string GenerateToken(long id)
    {
        return RandomString(10) + id * 3 + RandomString(id < 100 ? 50 : 100);
    }

    public static string GenerateScIdToken(long id)
    {
        return "#SC-" + "PU" + id + "/" + RandomString(10) + id * 3 + ":";
    }

    public static int GetPortFromDomain(string domain)
    {
        return Convert.ToInt32(domain[(domain.IndexOf(':') + 1)..]);
    }

    public static IPEndPoint GetFullyEndPointByDomain(string domain)
    {
        return new IPEndPoint(IPAddress.Parse(GetIpFromDomain(domain)), GetPortFromDomain(domain));
    }

    public static int GenerateRandomIntForBetween(int min, int max)
    {
        return new Random().Next(min, max + 1);
    }

    public static bool GetChanceByPercentage(int percentage)
    {
        return new Random().Next(0, 100) <= percentage;
    }

    public static string GetIpBySocket(Socket socket)
    {
        return socket.RemoteEndPoint!.ToString()?
            [..socket.RemoteEndPoint.ToString()!.IndexOf(":", StringComparison.Ordinal)];
    }

    public static int? GetPortBySocket(Socket socket)
    {
        return Convert.ToInt32(socket.RemoteEndPoint!.ToString()?
            [(socket.RemoteEndPoint.ToString()!.IndexOf(':') + 1)..]);
    }

    public static string ConvertStringToCamelCase(string str)
    {
        var result = new StringBuilder();
        {
            var capitalizeNext = false;
            {
                foreach (var currentChar in str)
                    if (currentChar == '_')
                    {
                        capitalizeNext = true;
                    }
                    else
                    {
                        if (capitalizeNext)
                        {
                            result.Append(char.ToUpper(currentChar));
                            capitalizeNext = false;
                        }
                        else
                        {
                            result.Append(char.ToLower(currentChar));
                        }
                    }
            }
        }

        return result.ToString();
    }

    public static object GetTypeAndSetOfGetDefaultValueToJson(FieldInfo field)
    {
        try
        {
            if (field.FieldType == typeof(string)) return "NULL";

            if (field.FieldType == typeof(int) || field.FieldType == typeof(long) ||
                field.FieldType == typeof(byte)) return -1;

            if (field.FieldType == typeof(bool)) return false;

            if (field.FieldType == typeof(double)) return (double)-1;

            if (field.FieldType == typeof(short)) return (short)-1;

            if (field.FieldType == typeof(float)) return -1f;

            if (field.FieldType == typeof(char)) return '\u0000';

            if (field.FieldType == typeof(Dictionary<object, object>)) return new Dictionary<object, object>();

            if (field.FieldType.IsArray)
            {
                var componentType = field.FieldType.GetElementType();
                {
                    if (componentType == typeof(int)) return Array.Empty<int>();

                    if (componentType == typeof(string)) return Array.Empty<string>();

                    if (componentType == typeof(char)) return Array.Empty<char>();

                    if (componentType == typeof(long)) return Array.Empty<long>();

                    if (componentType == typeof(byte)) return Array.Empty<byte>();

                    if (componentType == typeof(Dictionary<object, object>)) return new Dictionary<object, object>();
                }
            }
        }
        catch (Exception)
        {
            // ignoring
        }

        return null!;
    }

    public static string GetPacketNameByType(int type)
    {
        return JObject.Parse(Table.PacketInfo).ToObject<Dictionary<string, string>>()!.ContainsKey(type.ToString())
            ? JObject.Parse(Table.PacketInfo)[type.ToString()]?.ToString()
            : "none";
    }

    public static int GetPacketTypeByName(string name)
    {
        var jsonObject = JObject.Parse(Table.PacketInfo);

        foreach (var pair in jsonObject)
            if (pair.Value!.ToString() == name)
                return int.Parse(pair.Key);

        return 0;
    }

    public static int AngleMutation(int mutationStart, int mutationCounter, int mutationSpread, int mutationCount,
        int mutationStep)
    {
        mutationSpread = -mutationSpread + mutationStep;

        var d = mutationSpread != 0 ? -mutationSpread / 4 : -1;
        {
            for (var i = 0; i < mutationCounter; i++)
                d += mutationSpread != 0 ? mutationSpread / (2 * mutationCount) : 4;
        }

        return mutationStart + d + 360 / 100;
    }

    public static List<int> SumRepeatedElements(IEnumerable<int> inputList)
    {
        var enumerable = inputList as int[] ?? inputList.ToArray();
        {
            if (enumerable.Length < 3) return enumerable.ToList();
        }

        var elementSumDictionary = new Dictionary<int, int>();

        foreach (var number in enumerable.ToList().Where(number => !elementSumDictionary.TryAdd(number, number)))
            elementSumDictionary[number] += number;
        return elementSumDictionary.Values.ToList();
    }

    public static void Destructor(object @class)
    {
        var fields = @class.GetType().GetFields();
        {
            foreach (var field in fields)
                try
                {
                    field.SetValue(@class, GetTypeAndSetOfGetDefaultValueToJson(field));
                }
                catch (Exception)
                {
                    // ignoring
                }
        }
    }
}