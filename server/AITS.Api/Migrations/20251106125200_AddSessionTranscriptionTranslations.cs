using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTranscriptionTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Culture", "Key", "Value" },
                values: new object[,]
                {
                    { 105, "pl", "sessions.transcriptions.title", "Transkrypcje sesji" },
                    { 106, "en", "sessions.transcriptions.title", "Session transcriptions" },
                    { 107, "pl", "sessions.transcriptions.subtitle", "Każda nowa transkrypcja zastępuje poprzednią. Wybierz jedną z metod pozyskania tekstu rozmowy." },
                    { 108, "en", "sessions.transcriptions.subtitle", "Each new transcript replaces the previous one. Choose one of the methods to capture the conversation." },
                    { 109, "pl", "sessions.transcriptions.refresh", "Odśwież listę" },
                    { 110, "en", "sessions.transcriptions.refresh", "Refresh list" },
                    { 111, "pl", "sessions.transcriptions.realtime.title", "Nagrywanie z diarizacją w czasie rzeczywistym" },
                    { 112, "en", "sessions.transcriptions.realtime.title", "Real-time recording with diarization" },
                    { 113, "pl", "sessions.transcriptions.realtime.statusRecording", "Nagrywanie trwa" },
                    { 114, "en", "sessions.transcriptions.realtime.statusRecording", "Recording in progress" },
                    { 115, "pl", "sessions.transcriptions.realtime.status.disconnected", "Rozłączono" },
                    { 116, "en", "sessions.transcriptions.realtime.status.disconnected", "Disconnected" },
                    { 117, "pl", "sessions.transcriptions.realtime.status.connecting", "Łączenie..." },
                    { 118, "en", "sessions.transcriptions.realtime.status.connecting", "Connecting..." },
                    { 119, "pl", "sessions.transcriptions.realtime.status.recording", "Nagrywanie" },
                    { 120, "en", "sessions.transcriptions.realtime.status.recording", "Recording" },
                    { 121, "pl", "sessions.transcriptions.realtime.status.stopping", "Zamykanie..." },
                    { 122, "en", "sessions.transcriptions.realtime.status.stopping", "Stopping..." },
                    { 123, "pl", "sessions.transcriptions.realtime.status.error", "Błąd połączenia" },
                    { 124, "en", "sessions.transcriptions.realtime.status.error", "Error" },
                    { 125, "pl", "sessions.transcriptions.realtime.description", "Połącz się z Azure Speech i uzyskaj transkrypcję z diarizacją dla maksymalnie 3 osób. Wyniki aktualizują się na bieżąco." },
                    { 126, "en", "sessions.transcriptions.realtime.description", "Connect to Azure Speech and get a diarized transcript for up to 3 speakers. Results update continuously." },
                    { 127, "pl", "sessions.transcriptions.realtime.connecting", "Łączenie z usługą Azure..." },
                    { 128, "en", "sessions.transcriptions.realtime.connecting", "Connecting to Azure Speech..." },
                    { 129, "pl", "sessions.transcriptions.realtime.stop", "Zatrzymaj nagrywanie na żywo" },
                    { 130, "en", "sessions.transcriptions.realtime.stop", "Stop live recording" },
                    { 131, "pl", "sessions.transcriptions.realtime.stopping", "Zamykanie sesji..." },
                    { 132, "en", "sessions.transcriptions.realtime.stopping", "Closing session..." },
                    { 133, "pl", "sessions.transcriptions.realtime.retry", "Spróbuj ponownie" },
                    { 134, "en", "sessions.transcriptions.realtime.retry", "Try again" },
                    { 135, "pl", "sessions.transcriptions.realtime.start", "Rozpocznij nagrywanie na żywo" },
                    { 136, "en", "sessions.transcriptions.realtime.start", "Start live recording" },
                    { 137, "pl", "sessions.transcriptions.realtime.error", "Wystąpił błąd podczas nagrywania." },
                    { 138, "en", "sessions.transcriptions.realtime.error", "An error occurred while recording." },
                    { 139, "pl", "sessions.transcriptions.realtime.stopError", "Nie udało się zatrzymać nagrywania." },
                    { 140, "en", "sessions.transcriptions.realtime.stopError", "Failed to stop recording." },
                    { 141, "pl", "sessions.transcriptions.success", "Transkrypcja została zapisana (poprzednia wersja została zastąpiona)." },
                    { 142, "en", "sessions.transcriptions.success", "Transcription has been saved (the previous version was replaced)." },
                    { 143, "pl", "sessions.transcriptions.error", "Nie udało się przetworzyć pliku." },
                    { 144, "en", "sessions.transcriptions.error", "Could not process the file." },
                    { 145, "pl", "sessions.transcriptions.audioUploadTitle", "Transkrypcja z pliku audio (WAV/MP3)" },
                    { 146, "en", "sessions.transcriptions.audioUploadTitle", "Transcription from audio file (WAV/MP3)" },
                    { 147, "pl", "sessions.transcriptions.audioUploadHint", "Plik zostanie przesłany do Azure Speech i przetworzony z diarizacją." },
                    { 148, "en", "sessions.transcriptions.audioUploadHint", "The file will be sent to Azure Speech and processed with diarization." },
                    { 149, "pl", "sessions.transcriptions.audioUploadButton", "Wybierz plik audio" },
                    { 150, "en", "sessions.transcriptions.audioUploadButton", "Select audio file" },
                    { 151, "pl", "sessions.transcriptions.videoUploadTitle", "Transkrypcja z pliku wideo (MP4/MOV/MKV/AVI)" },
                    { 152, "en", "sessions.transcriptions.videoUploadTitle", "Transcription from video file (MP4/MOV/MKV/AVI)" },
                    { 153, "pl", "sessions.transcriptions.videoUploadHint", "Ścieżka audio zostanie wyodrębniona lokalnie i przetworzona w Azure Speech." },
                    { 154, "en", "sessions.transcriptions.videoUploadHint", "The audio track will be extracted locally and processed in Azure Speech." },
                    { 155, "pl", "sessions.transcriptions.videoUploadButton", "Wybierz plik wideo" },
                    { 156, "en", "sessions.transcriptions.videoUploadButton", "Select video file" },
                    { 157, "pl", "sessions.transcriptions.transcriptUploadTitle", "Wgraj gotową transkrypcję (TXT/VTT/SRT)" },
                    { 158, "en", "sessions.transcriptions.transcriptUploadTitle", "Upload final transcript (TXT/VTT/SRT)" },
                    { 159, "pl", "sessions.transcriptions.transcriptUploadHint", "Plik zostanie zapisany jako finalny transkrypt bez ponownego przetwarzania." },
                    { 160, "en", "sessions.transcriptions.transcriptUploadHint", "The file will be stored as the final transcript without further processing." },
                    { 161, "pl", "sessions.transcriptions.transcriptUploadButton", "Wybierz plik transkryptu" },
                    { 162, "en", "sessions.transcriptions.transcriptUploadButton", "Select transcript file" },
                    { 163, "pl", "sessions.transcriptions.currentTitle", "Aktualna transkrypcja" },
                    { 164, "en", "sessions.transcriptions.currentTitle", "Current transcript" },
                    { 165, "pl", "sessions.transcriptions.preview", "Pokaż podgląd" },
                    { 166, "en", "sessions.transcriptions.preview", "Show preview" },
                    { 167, "pl", "sessions.transcriptions.empty", "Brak zapisanej transkrypcji dla tej sesji." },
                    { 168, "en", "sessions.transcriptions.empty", "No transcript saved for this session." },
                    { 169, "pl", "sessions.transcriptions.previewTitle", "Podgląd transkrypcji" },
                    { 170, "en", "sessions.transcriptions.previewTitle", "Transcript preview" },
                    { 171, "pl", "sessions.transcriptions.download", "Pobierz źródło" },
                    { 172, "en", "sessions.transcriptions.download", "Download source" },
                    { 173, "pl", "sessions.transcriptions.source.manual", "Tekst ręczny" },
                    { 174, "en", "sessions.transcriptions.source.manual", "Manual text" },
                    { 175, "pl", "sessions.transcriptions.source.textFile", "Plik transkryptu" },
                    { 176, "en", "sessions.transcriptions.source.textFile", "Transcript file" },
                    { 177, "pl", "sessions.transcriptions.source.audioRecording", "Nagranie mikrofonem" },
                    { 178, "en", "sessions.transcriptions.source.audioRecording", "Microphone recording" },
                    { 179, "pl", "sessions.transcriptions.source.audioUpload", "Plik audio" },
                    { 180, "en", "sessions.transcriptions.source.audioUpload", "Audio file" },
                    { 181, "pl", "sessions.transcriptions.source.video", "Plik wideo" },
                    { 182, "en", "sessions.transcriptions.source.video", "Video file" },
                    { 183, "pl", "sessions.transcriptions.source.realtime", "Nagrywanie na żywo" },
                    { 184, "en", "sessions.transcriptions.source.realtime", "Live recording" },
                    { 185, "pl", "sessions.transcriptions.source.unknown", "Nieznane źródło" },
                    { 186, "en", "sessions.transcriptions.source.unknown", "Unknown source" },
                    { 187, "pl", "common.close", "Zamknij" },
                    { 188, "en", "common.close", "Close" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 143);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 144);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 153);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 154);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 155);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 156);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 157);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 158);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 159);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 160);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 161);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 162);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 163);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 164);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 165);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 166);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 167);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 168);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 169);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 170);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 171);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 172);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 173);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 174);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 175);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 176);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 177);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 178);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 179);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 180);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 181);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 182);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 183);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 184);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 185);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 186);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 187);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 188);
        }
    }
}
