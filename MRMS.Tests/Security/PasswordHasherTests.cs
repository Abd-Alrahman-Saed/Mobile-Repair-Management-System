using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MRMS.Application.Security;

namespace MRMS.Tests.Security;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_then_VerifyPbkdf2_round_trips()
    {
        var (hash, salt) = PasswordHasher.Hash("correct horse");

        PasswordHasher.VerifyPbkdf2("correct horse", hash, salt).Should().BeTrue();
        PasswordHasher.VerifyPbkdf2("wrong horse", hash, salt).Should().BeFalse();
    }

    [Fact]
    public void Hash_uses_a_fresh_salt_each_call()
    {
        var (h1, s1) = PasswordHasher.Hash("same");
        var (h2, s2) = PasswordHasher.Hash("same");

        s1.Should().NotBe(s2);
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void VerifyLegacySha256_matches_the_old_unsalted_hex_format()
    {
        var legacy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("admin123")));

        PasswordHasher.VerifyLegacySha256("admin123", legacy).Should().BeTrue();
        PasswordHasher.VerifyLegacySha256("nope", legacy).Should().BeFalse();
    }

    [Fact]
    public void VerifyPbkdf2_returns_false_for_garbage_input_instead_of_throwing()
    {
        PasswordHasher.VerifyPbkdf2("x", "not-base64!!", "also-not").Should().BeFalse();
    }
}
