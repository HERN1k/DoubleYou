/*  
    MIT License

    Copyright (c) 2024 Vlad Hirnyk (HERN1k)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE. 
*/

namespace DoubleYou.Utilities
{
    public sealed class Constants
    {
        /* 
            Exceptions 
         */
        public const string AN_UNEXPECTED_ERROR_OCCURRED = "An unexpected error occurred: ";
        public const string MAIN_WINDOW_RESOLUTION_ERROR_MESSAGE = "Failed to get MainWindow instance from dependency container";
        public const string INTRODUCTION_WINDOW_RESOLUTION_ERROR_MESSAGE = "Failed to get IntroductionWindow instance from dependency container";
        public const string LOGGER_RESOLUTION_ERROR_MESSAGE = "Failed to get ILogger<App> instance from dependency container";
        public const string FAILED_TO_RETRIEVE_THE_WINDOWS_VERSION = "Failed to retrieve the Windows version from AnalyticsInfo.VersionInfo.DeviceFamilyVersion";
        public const string INVALID_HEX_FORMAT = "Invalid hex format. Use #RRGGBB or #RRGGBBAA";
        public const string APPLICATION_RESOURCES_ARE_NOT_INITIALIZED = "Application resources are not initialized";
        public const string PAGE_IS_NOT_REGISTER_IN_DI = "page is not registered in the DI container";
        public const string FAILED_SET_APPLICATION_CULTURE = "Failed to set application culture";
        public const string USER_NOT_FOUND = "User data not found";
        public const string CULTURE_CODE_NOT_FOUND_OR_INVALID = "Culture code not found or invalid";
        public const string FAILED_TO_LOAD_RESOURCES_FOR_NEW_CULTURE = "Failed to load resources for new culture";
        public const string FAILED_TO_ADD_TASK_TO_UI_THREAD = "Failed to add task to UI thread";
        public const string UNABLE_TO_ACCESS_DISPATCHERQUEUE = "Unable to access DispatcherQueue";
        public const string REQUEST_FAILED_AFTER_MULTIPLE_ATTEMPTS = "Request failed after multiple attempts";
        public const string NO_DATA_RECEIVED = "No data received";
        public const string INVALID_FILE_NAME = "Invalid file name";
        public const string FILE_NOT_EXISTS = "File not exists";
        public const string ERROR_READING_FILE = "Error reading file";
        public const string INVALID_DIRECTORY_PATH = "Invalid directory path";
        public const string ERROR_IMPORTING_DUMP = "Error importing dump";

        /*
            Application keys
         */
        public const string CURRENT_CULTURE = "CurrentCulture";
        public const string SELECT_ITEM = "SelectItem";
        public const string ENGLISH_LANGUAGE_KEY = "en-US";
        public const string UKRAINIAN_LANGUAGE_KEY = "uk-UA";
        public const string RUSSIAN_LANGUAGE_KEY = "ru-RU";
        public const string POLISH_LANGUAGE_KEY = "pl-PL";
        public const string GERMAN_LANGUAGE_KEY = "de-DE";
        public const string HEBREW_LANGUAGE_KEY = "he-IL";
        public const string PROGRAMMING_TOPIC_KEY = "Programming";
        public const string TECHNOLOGY_TOPIC_KEY = "Technology";
        public const string EDUCATION_TOPIC_KEY = "Education";
        public const string FINANCE_TOPIC_KEY = "Finance";
        public const string MEDICINE_TOPIC_KEY = "Medicine";
        public const string LAW_TOPIC_KEY = "Law";
        public const string TRAVEL_TOPIC_KEY = "Travel";
        public const string HEALTH_TOPIC_KEY = "Health";
        public const string RECOVER_DATA_KEY = "RecoverData";
        public const string PART_OF_TRANSLATE_KEY = ":PART_OF_TRANSLATE_KEY:";
        public const string ANY_WORDS_KEY = ":ANY_WORDS_KEY:";
        public const string WORDS_NOT_LEARNED_KEY = ":WORDS_NOT_LEARNED_KEY:";
        public const string COUNT_WORDS_NOT_LEARNED_KEY = ":COUNT_WORDS_NOT_LEARNED_KEY:";
        public const string WORDS_LEARNED_KEY = ":WORDS_LEARNED_KEY:";
        public const string WORDS_LEARNED_FOR_REPETITION_KEY = ":WORDS_LEARNED_FOR_REPETITION_KEY:";
        public const string COUNT_WORDS_LEARNED_KEY = ":COUNT_WORDS_LEARNED_KEY:";
        public const string WORDS_NOT_LEARNED_BY_TOPIC_KEY = ":WORDS_NOT_LEARNED_BY_TOPIC_KEY:";
        public const string COUNT_WORDS_NOT_LEARNED_BY_TOPIC_KEY = ":COUNT_WORDS_NOT_LEARNED_BY_TOPIC_KEY:";
        public const string WORDS_LEARNED_BY_TOPIC_KEY = ":WORDS_LEARNED_BY_TOPIC_KEY:";
        public const string COUNT_WORDS_LEARNED_BY_TOPIC_KEY = ":COUNT_WORDS_LEARNED_BY_TOPIC_KEY:";
        public const string COUNT_WORDS_LEARNED_BY_TIME_SPAN_KEY = ":COUNT_WORDS_LEARNED_BY_TIME_SPAN_KEY:";
        public const string SETTINGS_KEY = "Settings";
        public const string APPLICATION_LANGUAGE_KEY = "ApplicationLanguage";
        public const string TRANSLATION_LANGUAGE_KEY = "TranslationLanguage";
        public const string FAVORITE_TOPIC_KEY = "FavoriteTopic";
        public const string CREATING_DATA_BACKUP_KEY = "CreatingDataBackup";
        public const string SELECT_FOLDER_KEY = "SelectFolder";
        public const string INFORMATION_KEY = "Information";
        public const string OK_KEY = "OK";
        public const string DATA_BACKUP_COMPLETED_SUCCESSFULLY_KEY = "DataBackupCompletedSuccessfully";
        public const string FACTORY_RESET_KEY = "FactoryReset";
        public const string RESET_KEY = "Reset";
        public const string WARNING_KEY = "WARNING";
        public const string ARE_YOU_SURE_YOU_WANT_RESET_ALL_SETTINGS_KEY = "AreYouSureYouWantResetAllSettings";
        public const string YES_KEY = "Yes";
        public const string NEW_WORDS_KEY = "NewWords";
        public const string WELL_DONE_KEY = "WellDone";
        public const string WORDS_SAVED_AS_LEARNED_KEY = "WordsSavedAsLearned";
        public const string WORDS_LEARNED_TITLE_KEY = "LearnedWordsTitle";
        public const string REPETITION_OF_WORDS_TITLE_KEY = "RepetitionOfWordsTitle";
        public const string OPEN_ALL_KEY = "OpenAll";
        public const string HIDE_ALL_KEY = "HideAll";
        public const string PAGE_ELEMENT_TAG = "PAGE_ELEMENT_TAG:";
        public const string ADD_KEY = "Add";
        public const string INSTALLING_VOICES_KEY = "InstallingVoices";
        public const string DEVICE_NOT_HAVE_VOICES_KEY = "DeviceNotHaveVoices";


        /* 
            Main
         */
        public const string DATABASE_RESTORED_FROM_DUMP = "Database restored from dump";
        public const string APP_INITIALIZED = "The application has been initialized";
        public const string APP_DISPLAY_NAME = "Double You";
        public const string SLOGAN = "Words for everyone";
        public const string BACKUP_SQL_DB_FILE_NAME = "backup.sql";
        public const string SQL_DB_FILE_NAME = "DoubleYou.db";
        public const string LOGO_PATH = "ms-appx:///Assets/Logos/logo.png";
        public const string API_WORD_SEPARATOR_REQUEST = ". ";
        public const string API_WORD_SEPARATOR_RESPONSE = ".";
        public const string SETTINGS_URI_SPEECH = "ms-settings:speech";
        public const string EF_MIGRATION_HISTORY_TABLE_NAME = "__EFMigrationsHistory";
    }
}