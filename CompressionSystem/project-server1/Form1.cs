using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace project_server1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        TcpListener server;

        // Create Server
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                server = new TcpListener(
                    IPAddress.Parse("127.0.0.1"),
                    9050);

                listBox1.Items.Add("Server Created");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Start Server + Accept Multiple Clients
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                server.Start();
                listBox1.Items.Add("Server Started");

                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            Invoke(new Action(() =>
                            {
                                listBox1.Items.Add("Waiting For Client...");
                            }));

                            Socket client = server.AcceptSocket();

                            Invoke(new Action(() =>
                            {
                                listBox1.Items.Add("Client Connected");
                            }));

                            // كل Client في Thread مستقل
                            Task.Run(() =>
                            {
                                ReceiveAndCompress(client);
                            });
                        }
                        catch (Exception ex)
                        {
                            Invoke(new Action(() =>
                            {
                                listBox1.Items.Add("ERROR : " + ex.Message);
                            }));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Receive + Compress + Send (Per Client)
        private void ReceiveAndCompress(Socket client)
        {
            try
            {
                NetworkStream stream = new NetworkStream(client);

                // =========================
                // Receive File Size
                // =========================
                byte[] sizeBuffer = new byte[8];
                int totalRead = 0;

                while (totalRead < 8)
                {
                    int read = stream.Read(
                        sizeBuffer,
                        totalRead,
                        8 - totalRead);

                    if (read == 0)
                        throw new Exception("Connection Closed");

                    totalRead += read;
                }

                long fileSize = BitConverter.ToInt64(sizeBuffer, 0);

                Invoke(new Action(() =>
                {
                    listBox1.Items.Add("File Size : " + fileSize);
                }));

                // =========================
                // Receive File
                // =========================
                byte[] fileData = new byte[fileSize];
                int received = 0;

                while (received < fileSize)
                {
                    int read = stream.Read(
                        fileData,
                        received,
                        (int)(fileSize - received));

                    if (read == 0)
                        throw new Exception("Connection Lost");

                    received += read;
                }

                Invoke(new Action(() =>
                {
                    listBox1.Items.Add("File Received Successfully");
                }));

                // =========================
                // Compress File
                // =========================
                byte[] compressedData;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
                    {
                        gzip.Write(fileData, 0, fileData.Length);
                    }

                    compressedData = ms.ToArray();
                }

                Invoke(new Action(() =>
                {
                    listBox1.Items.Add("Compressed Size : " + compressedData.Length);
                }));

                // =========================
                // Send Compressed Size
                // =========================
                byte[] compressedSize =
                    BitConverter.GetBytes((long)compressedData.Length);

                stream.Write(compressedSize, 0, compressedSize.Length);

                // =========================
                // Send Compressed File
                // =========================
                stream.Write(compressedData, 0, compressedData.Length);
                stream.Flush();

                Invoke(new Action(() =>
                {
                    listBox1.Items.Add("Compressed File Sent");
                }));

                client.Close();
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    listBox1.Items.Add("ERROR : " + ex.Message);
                }));
            }
        }

        // Stop Server
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                server?.Stop();
                listBox1.Items.Add("Server Closed");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}