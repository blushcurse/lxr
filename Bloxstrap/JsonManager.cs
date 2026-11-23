namespace Bloxstrap
{
    public class JsonManager<T> where T : class, new()
    {
        protected T _prop = new();

        public virtual T Prop
        {
            get => _prop;
            set => _prop = value;
        }

        /// <summary>
        /// The file hash when last retrieved from disk
        /// </summary>
        public string? LastFileHash { get; private set; }

        public bool Loaded { get; protected set; } = false;

        public virtual string ClassName { get; }

        public virtual string FileName => $"{ClassName}.json";

        public virtual string FileLocation => Path.Combine(Paths.Base, FileName);

        public bool IsSaved => File.Exists(FileLocation);

        public virtual string LOG_IDENT_CLASS => $"JsonManager<{ClassName}>";

        public JsonManager(string? className = null)
        {
            ClassName = string.IsNullOrEmpty(className) ? typeof(T).Name : className;
        }

        public virtual bool Load(bool alertFailure = true)
        {
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Load";

            App.Logger.WriteLine(LOG_IDENT, $"Loading from {FileLocation}...");

            try
            {
                if (File.Exists(FileLocation))
                {
                    string contents = File.ReadAllText(FileLocation);

                    T? settings = DeserializeOrRecover(ref contents);

                    if (settings is null)
                        throw new ArgumentNullException("Deserialization returned null");

                    _prop = settings;
                    Loaded = true;
                    LastFileHash = MD5Hash.FromString(contents);

                    App.Logger.WriteLine(LOG_IDENT, "Loaded successfully!");

                    return true;
                }
                else
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Could not find {FileLocation}.");
                    Loaded = true;

                    return false;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load!");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (alertFailure)
                {
                    string message = "";

                    if (ClassName == nameof(Settings))
                        message = Strings.JsonManager_SettingsLoadFailed;
                    else if (ClassName == nameof(FastFlagManager))
                        message = Strings.JsonManager_FastFlagsLoadFailed;

                    if (!String.IsNullOrEmpty(message))
                        Frontend.ShowMessageBox($"{message}\n\n{ex.Message}", System.Windows.MessageBoxImage.Warning);

                    try
                    {
                        // Keep the unreadable file for troubleshooting. The ".bak" file is the last known-good copy
                        // written by Save(), so it must not be overwritten here.
                        File.Copy(FileLocation, FileLocation + ".corrupt", true);
                    }
                    catch (Exception copyEx)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to create backup file: {FileLocation}.bak");
                        App.Logger.WriteException(LOG_IDENT, copyEx);
                    }
                }

                Loaded = true;
                Save();

                return false;
            }
        }

        /// <summary>
        /// Parses the main file. If it is unreadable, falls back to the last known-good ".bak" copy.
        /// </summary>
        private T? DeserializeOrRecover(ref string contents)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(contents);
            }
            catch (JsonException) when (File.Exists(FileLocation + ".bak"))
            {
                string backup = File.ReadAllText(FileLocation + ".bak");
                T? recovered = JsonSerializer.Deserialize<T>(backup);

                App.Logger.WriteLine($"{LOG_IDENT_CLASS}::Load", "Main file was unreadable, recovered from the last known-good copy");
                contents = backup;

                // restore the readable copy as the main file so disk and memory agree
                File.Copy(FileLocation + ".bak", FileLocation, true);
                return recovered;
            }
        }

        public virtual void Save()
        {
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Save";

            Directory.CreateDirectory(Path.GetDirectoryName(FileLocation)!);

            try
            {
                string contents = JsonSerializer.Serialize(Prop, new JsonSerializerOptions { WriteIndented = true });
                string hash = MD5Hash.FromString(contents);

                // nothing changed since the last write, so don't touch the disk
                if (hash == LastFileHash && File.Exists(FileLocation))
                    return;

                App.Logger.WriteLine(LOG_IDENT, $"Saving to {FileLocation}...");

                // write to a temporary file first, then swap it in so a crash can never leave a half-written file
                string temp = FileLocation + ".tmp";
                File.WriteAllText(temp, contents);

                if (File.Exists(FileLocation))
                    File.Replace(temp, FileLocation, FileLocation + ".bak", true);
                else
                    File.Move(temp, FileLocation);

                LastFileHash = hash;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to save");
                App.Logger.WriteException(LOG_IDENT, ex);

                string errorMessage = string.Format(Resources.Strings.Bootstrapper_JsonManagerSaveFailed, ClassName, ex.Message);
                Frontend.ShowMessageBox(errorMessage, System.Windows.MessageBoxImage.Warning);

                return;
            }

            App.Logger.WriteLine(LOG_IDENT, "Save complete!");
        }

        public virtual void Delete()
        {
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Delete";

            try
            {
                if (File.Exists(FileLocation))
                {
                    File.Delete(FileLocation);

                    Loaded = false;
                    App.Logger.WriteLine(LOG_IDENT, "Delete complete!");
                }
                else
                {
                    App.Logger.WriteLine(LOG_IDENT, "File does not exist on disk");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to delete");
                App.Logger.WriteException(LOG_IDENT, ex);

                // should we notify?
            }
        }

        /// <summary>
        /// Is the file on disk different to the one deserialised during this session?
        /// </summary>
        public bool HasFileOnDiskChanged()
        {
            // check if a file has been created since launch
            if (string.IsNullOrEmpty(LastFileHash) && File.Exists(FileLocation))
                return true;

            return LastFileHash != MD5Hash.FromFile(FileLocation);
        }
    }

    /// <summary>
    /// <see cref="JsonManager{T}"/> that will automatically load in the JSON if it has not been already
    /// </summary>
    /// <typeparam name="T">Class</typeparam>
    public class LazyJsonManager<T> : JsonManager<T> where T : class, new()
    {
        public override T Prop
        {
            get
            {
                if (!Loaded)
                    Load();

                return _prop;
            }
            set
            {
                _prop = value;
                Loaded = true;
            }
        }

        public LazyJsonManager(string? className)
            : base(className)
        {
        }
    }
}
