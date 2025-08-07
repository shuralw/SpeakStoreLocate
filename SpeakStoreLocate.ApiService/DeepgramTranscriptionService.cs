using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.Listen.v1.REST;

namespace SpeakStoreLocate.ApiService;

public class DeepgramTranscriptionService : ITranscriptionService
{
    private readonly IListenRESTClient deepgramClient;
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;

    public DeepgramTranscriptionService(IConfiguration configuration)
    {
        // Set "DEEPGRAM_API_KEY" environment variable to your Deepgram API Key

        this.deepgramClient = ClientFactory.CreateListenRESTClient();

        var awsOptions = configuration.GetSection("AWS");
        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(awsOptions["AccessKey"], awsOptions["SecretKey"]);
        var awsRegion = Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"]);


        this._bucketName = configuration["AWS:BucketName"];

        var s3Config = new AmazonS3Config { RegionEndpoint = awsRegion };
        _s3Client = new AmazonS3Client(awsCredentials, s3Config);
    }

    public async Task<string> TranscriptAudioAsync_Local(AudioUploadRequest request)
    {
        byte[] audioBytes;
        using (var ms = new MemoryStream())
        {
            await request.AudioFile.CopyToAsync(ms);
            audioBytes = ms.ToArray();
        }

        var response = await deepgramClient.TranscribeFile(
            audioBytes,
            new PreRecordedSchema()
            {
                Model = "nova-2",
                SmartFormat = true,
            });


        Console.WriteLine($"\n\n{response}\n\n");
        Console.WriteLine("Press any key to exit...");
        return "";
        // Teardown Library Library.Terminate();

    }
    public async Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {

        byte[] audioBytes;
        using (var ms = new MemoryStream())
        {
            await request.AudioFile.CopyToAsync(ms);
            audioBytes = ms.ToArray();
        }

        var response = await deepgramClient.TranscribeFile(
            audioBytes,
            new PreRecordedSchema()
            {
                Model = "nova-3-general",
                Punctuate = true,
                Language = "multi",
                SmartFormat = true,
            });

        // 1. Für jeden Kanal die beste Alternative herausholen
        var bestTranscriptionPerChannel = response
            .Results
            .Channels
            .Select(channel =>
                channel.Alternatives
                    .OrderByDescending(alt => alt.Confidence)
                    .First()
                    .Transcript
            );

        // 2. Alle Kanal-Transkripte zu einem Gesamtstring verbinden
        string transcriptText = string.Join(" ", bestTranscriptionPerChannel);


        return transcriptText;
    }
}