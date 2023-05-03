﻿namespace AWS.API.Model
{
    public class ResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public int ErrorCode { get; set; }
    }
        
}
