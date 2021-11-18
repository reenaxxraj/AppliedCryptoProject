using AppliedCryptoProject;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

FileManager fileManager = new FileManager();

Console.WriteLine(fileManager.generateRSAKeyPair().ToString());