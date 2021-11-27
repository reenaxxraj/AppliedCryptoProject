using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace AppliedCryptoProject
{
    public record CreateIdentitySignedRequest(CreateIdentityRequest Request, string Signature);
    public record CreateIdentityRequest(RSAPubKey PublicKey, string Username);
    public record CreateIdentityResponse(RSAPubKey PublicKey, string TaggedUsername);
    public record RSAPubKey(string Modulus, string Exponent);

    public record SharingFileSignedRequest(SharingFileRequest Request, string Signature);

    public record SharingFileRequest(string URL, UsernameKeyPair[] TaggedUsernamesToShareWith, string CallerTaggedUsername);

    public record UsernameKeyPair(string EncryptedKey, string TaggedUsername);
    public record SharingFileResponse(string URL, IEnumerable<string> TaggedUsernames);

    public record UnsharingFileSignedRequest(UnsharingFileRequest Request, string Signature);

    public record UnsharingFileRequest(string URL, string[] TaggedUsernamesToRemove, string CallerTaggedUsername);


    public record SubmitFileSignedRequest(SubmitFileRequest Request, string Signature);
    public record SubmitFileRequest(string EncryptedFile, string EncryptedKey, string TaggedUsername);
    public record SubmitFileResponse(string URL);

    public record GetFileResponse(string EncryptedFile, string EncryptedKey);
    public record GetFileRequest(string Url, string TaggedUsername);

    public record DeleteFileSignedRequest(DeleteFileRequest Request, string Signature);
    public record DeleteFileRequest(string URL, string TaggedUsername);




}
