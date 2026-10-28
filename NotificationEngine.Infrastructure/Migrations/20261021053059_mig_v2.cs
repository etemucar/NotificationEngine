using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NotificationEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "notification_engine",
                table: "message_template",
                columns: new[] { "message_template_id", "body", "channel", "code", "create_date", "created_by", "culture", "is_deleted", "organization_id", "status", "subject", "update_date", "updated_by" },
                values: new object[,]
                {
                    { 1001, "Sifre sifirlama kodunuz: {{code}}. {{expiresInMinutes}} dakika gecerlidir. Kodu kimseyle paylasmayin.", 2, "PASSWORD_RESET", null, null, "tr-TR", 0, null, 3, null, null, null },
                    { 1002, "Merhaba,\n\nHesabınız için bir şifre sıfırlama talebi aldık. Şifrenizi yenilemek için aşağıdaki bağlantıyı kullanın:\n\n{{resetLink}}\n\nBu bağlantı {{expiresInMinutes}} dakika boyunca geçerlidir. Bu talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz.\n", 1, "PASSWORD_RESET", null, null, "tr-TR", 0, null, 3, "Şifre sıfırlama talebi", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "notification_engine",
                table: "message_template",
                keyColumn: "message_template_id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                schema: "notification_engine",
                table: "message_template",
                keyColumn: "message_template_id",
                keyValue: 1002);
        }
    }
}
