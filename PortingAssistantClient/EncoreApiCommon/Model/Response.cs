using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EncoreApiCommon.Model
{
    public class Response<T, E>
    {
        public class ResponseStatus
        {
            public enum StatusCode
            {
                Success,
                Failure
            }

            [JsonConverter(typeof(StringEnumConverter))]
            public StatusCode Status { get; set; }
            public Exception Error { get; set; }
        }

        public ResponseStatus Status { get; set;  }

        public T Value { get; set; }
        public E ErrorValue { get; set; }

        public static ResponseStatus Success()
        {
            return new ResponseStatus
            {
                Status = ResponseStatus.StatusCode.Success
            };
        }

        public static ResponseStatus Failed(Exception ex)
        {
            return new ResponseStatus
            {
                Status = ResponseStatus.StatusCode.Failure,
                Error = ex
            };
        }
    }
}
