//using Newtonsoft.Json;
//using ARMCommon.Interface;

//namespace AxStructDef.Lib
//{
    //public class IDbHelperConverter : JsonConverter<IDbHelper>
    //{
    //    public override bool CanWrite => false;

    //    public override void WriteJson(JsonWriter writer, IDbHelper value, JsonSerializer serializer)
    //    {
    //        writer.WriteNull(); // Don't serialize IDbHelper
    //    }

    //    public override IDbHelper ReadJson(JsonReader reader, Type objectType, IDbHelper existingValue, bool hasExistingValue, JsonSerializer serializer)
    //    {
    //        // Skip the DB helper during deserialization
    //        reader.Skip();
    //        return null; // It will be recreated when needed
    //    }
    //}

    //public class IRedisCacheHelperConverter : JsonConverter<IRedisCacheHelper>
    //{
    //    public override bool CanWrite => false;

    //    public override void WriteJson(JsonWriter writer, IRedisCacheHelper value, JsonSerializer serializer)
    //    {
    //        writer.WriteNull(); // Don't serialize IDbHelper
    //    }

    //    public override IRedisCacheHelper ReadJson(JsonReader reader, Type objectType, IRedisCacheHelper existingValue, bool hasExistingValue, JsonSerializer serializer)
    //    {
    //        // Skip the DB helper during deserialization
    //        reader.Skip();
    //        return null; // It will be recreated when needed
    //    }
    //}

//}