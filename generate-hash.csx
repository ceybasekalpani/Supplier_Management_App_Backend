#r "nuget: BCrypt.Net-Next, 4.0.3"
using BCrypt.Net;

var password = "1234";
var hash = BCrypt.HashPassword(password, 11);
Console.WriteLine(hash);
