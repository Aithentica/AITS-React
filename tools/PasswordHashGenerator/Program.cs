using Microsoft.AspNetCore.Identity;

var password = "Piotr1234!";
var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null!, password);

Console.WriteLine(hash);
