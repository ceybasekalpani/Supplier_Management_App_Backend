using System;
using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "1234";
        string hash = BCrypt.HashPassword(password, workFactor: 11);
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
        Console.WriteLine("Copy this hash to your database.");
    }
}
