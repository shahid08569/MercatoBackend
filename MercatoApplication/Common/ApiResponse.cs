using System;
using System.Collections.Generic;
using System.Text;

namespace MercatoApplication.Common;
// Generic version - jab data bhejna ho (jaise product list, user info)
public class ApiResponse<T> 
{ 
    public bool Success { get; set; } 
    public T? Data { get; set; } 
    public string? Error { get; set; } 
    public static ApiResponse<T> SuccessResponse(T data) => new()
    { 
        Success = true, Data = data 
    };
    public static ApiResponse<T> FailureResponse(string error) => new() 
    { 
        Success = false, Error = error 
    };
} 
// Non-generic version - jab koi data nahi bhejna (jaise "delete successful")
public class ApiResponse 
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public static ApiResponse SuccessResponse() => new()
    {
        Success = true }; 
    public static ApiResponse FailureResponse(string error) => new()
    { 
        Success = false, Error = error 
    }; 
}
