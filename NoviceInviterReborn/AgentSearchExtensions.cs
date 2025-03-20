using NoviceInviterReborn;
using System;

public static unsafe class AgentSearchExtensions
{
    public static void SetOnlyOneRegion(IntPtr agent, int regionNumber)
    {
        if (regionNumber < 1 || regionNumber > 11)
            throw new ArgumentOutOfRangeException(nameof(regionNumber), "Region number must be between 1 and 11");

        var agentStruct = (AgentSearch*)agent;

        // Set region 1
        agentStruct->Area1Byte1 = (byte)(regionNumber == 1 ? 1 : 0);
        agentStruct->Area1Byte2 = (byte)(regionNumber == 1 ? 1 : 0);
        agentStruct->Area1Byte3 = (byte)(regionNumber == 1 ? 1 : 0);
        agentStruct->Area1Byte4 = (byte)(regionNumber == 1 ? 1 : 0);
        agentStruct->Area1Byte5 = (byte)(regionNumber == 1 ? 1 : 0);

        // Set region 2
        agentStruct->Area2Byte1 = (byte)(regionNumber == 2 ? 1 : 0);
        agentStruct->Area2Byte2 = (byte)(regionNumber == 2 ? 1 : 0);
        agentStruct->Area2Byte3 = (byte)(regionNumber == 2 ? 1 : 0);
        agentStruct->Area2Byte4 = (byte)(regionNumber == 2 ? 1 : 0);
        agentStruct->Area2Byte5 = (byte)(regionNumber == 2 ? 1 : 0);

        // Set region 3
        agentStruct->Area3Byte1 = (byte)(regionNumber == 3 ? 1 : 0);
        agentStruct->Area3Byte2 = (byte)(regionNumber == 3 ? 1 : 0);
        agentStruct->Area3Byte3 = (byte)(regionNumber == 3 ? 1 : 0);
        agentStruct->Area3Byte4 = (byte)(regionNumber == 3 ? 1 : 0);
        agentStruct->Area3Byte5 = (byte)(regionNumber == 3 ? 1 : 0);

        // Set region 4
        agentStruct->Area4Byte1 = (byte)(regionNumber == 4 ? 1 : 0);
        agentStruct->Area4Byte2 = (byte)(regionNumber == 4 ? 1 : 0);
        agentStruct->Area4Byte3 = (byte)(regionNumber == 4 ? 1 : 0);
        agentStruct->Area4Byte4 = (byte)(regionNumber == 4 ? 1 : 0);
        agentStruct->Area4Byte5 = (byte)(regionNumber == 4 ? 1 : 0);

        // Set region 5
        agentStruct->Area5Byte1 = (byte)(regionNumber == 5 ? 1 : 0);
        agentStruct->Area5Byte2 = (byte)(regionNumber == 5 ? 1 : 0);
        agentStruct->Area5Byte3 = (byte)(regionNumber == 5 ? 1 : 0);
        agentStruct->Area5Byte4 = (byte)(regionNumber == 5 ? 1 : 0);
        agentStruct->Area5Byte5 = (byte)(regionNumber == 5 ? 1 : 0);

        // Set region 6
        agentStruct->Area6Byte1 = (byte)(regionNumber == 6 ? 1 : 0);
        agentStruct->Area6Byte2 = (byte)(regionNumber == 6 ? 1 : 0);
        agentStruct->Area6Byte3 = (byte)(regionNumber == 6 ? 1 : 0);
        agentStruct->Area6Byte4 = (byte)(regionNumber == 6 ? 1 : 0);
        agentStruct->Area6Byte5 = (byte)(regionNumber == 6 ? 1 : 0);

        // Set region 7
        agentStruct->Area7Byte1 = (byte)(regionNumber == 7 ? 1 : 0);
        agentStruct->Area7Byte2 = (byte)(regionNumber == 7 ? 1 : 0);
        agentStruct->Area7Byte3 = (byte)(regionNumber == 7 ? 1 : 0);
        agentStruct->Area7Byte4 = (byte)(regionNumber == 7 ? 1 : 0);
        agentStruct->Area7Byte5 = (byte)(regionNumber == 7 ? 1 : 0);

        // Set region 8
        agentStruct->Area8Byte1 = (byte)(regionNumber == 8 ? 1 : 0);
        agentStruct->Area8Byte2 = (byte)(regionNumber == 8 ? 1 : 0);
        agentStruct->Area8Byte3 = (byte)(regionNumber == 8 ? 1 : 0);
        agentStruct->Area8Byte4 = (byte)(regionNumber == 8 ? 1 : 0);
        agentStruct->Area8Byte5 = (byte)(regionNumber == 8 ? 1 : 0);

        // Set region 9
        agentStruct->Area9Byte1 = (byte)(regionNumber == 9 ? 1 : 0);
        agentStruct->Area9Byte2 = (byte)(regionNumber == 9 ? 1 : 0);
        agentStruct->Area9Byte3 = (byte)(regionNumber == 9 ? 1 : 0);
        agentStruct->Area9Byte4 = (byte)(regionNumber == 9 ? 1 : 0);
        agentStruct->Area9Byte5 = (byte)(regionNumber == 9 ? 1 : 0);

        // Set region 10
        agentStruct->Area10Byte1 = (byte)(regionNumber == 10 ? 1 : 0);
        agentStruct->Area10Byte2 = (byte)(regionNumber == 10 ? 1 : 0);
        agentStruct->Area10Byte3 = (byte)(regionNumber == 10 ? 1 : 0);
        agentStruct->Area10Byte4 = (byte)(regionNumber == 10 ? 1 : 0);
        agentStruct->Area10Byte5 = (byte)(regionNumber == 10 ? 1 : 0);

        // Set region 11
        agentStruct->Area11Byte1 = (byte)(regionNumber == 11 ? 1 : 0);
        agentStruct->Area11Byte2 = (byte)(regionNumber == 11 ? 1 : 0);
        agentStruct->Area11Byte3 = (byte)(regionNumber == 11 ? 1 : 0);
        agentStruct->Area11Byte4 = (byte)(regionNumber == 11 ? 1 : 0);
        agentStruct->Area11Byte5 = (byte)(regionNumber == 11 ? 1 : 0);
    }

    public static AgentSearch.BackupData BackupValues(IntPtr agent)
    {
        var agentStruct = (AgentSearch*)agent;
        var backup = new AgentSearch.BackupData();

        // Backup basic fields
        backup.OnlineStatusLeft = agentStruct->OnlineStatusLeft;
        backup.OnlineStatusRight = agentStruct->OnlineStatusRight;
        backup.ClassSearch = agentStruct->ClassSearchRow1;
        backup.MinLevel = agentStruct->MinLevel;
        backup.MaxLevel = agentStruct->MaxLevel;
        backup.Language = agentStruct->Language;
        backup.Company = agentStruct->Company;

        // Backup area data
        // Area 1
        backup.AreaData[0] = agentStruct->Area1Byte1;
        backup.AreaData[1] = agentStruct->Area1Byte2;
        backup.AreaData[2] = agentStruct->Area1Byte3;
        backup.AreaData[3] = agentStruct->Area1Byte4;
        backup.AreaData[4] = agentStruct->Area1Byte5;

        // Area 2
        backup.AreaData[5] = agentStruct->Area2Byte1;
        backup.AreaData[6] = agentStruct->Area2Byte2;
        backup.AreaData[7] = agentStruct->Area2Byte3;
        backup.AreaData[8] = agentStruct->Area2Byte4;
        backup.AreaData[9] = agentStruct->Area2Byte5;

        // Area 3
        backup.AreaData[10] = agentStruct->Area3Byte1;
        backup.AreaData[11] = agentStruct->Area3Byte2;
        backup.AreaData[12] = agentStruct->Area3Byte3;
        backup.AreaData[13] = agentStruct->Area3Byte4;
        backup.AreaData[14] = agentStruct->Area3Byte5;

        // Area 4
        backup.AreaData[15] = agentStruct->Area4Byte1;
        backup.AreaData[16] = agentStruct->Area4Byte2;
        backup.AreaData[17] = agentStruct->Area4Byte3;
        backup.AreaData[18] = agentStruct->Area4Byte4;
        backup.AreaData[19] = agentStruct->Area4Byte5;

        // Area 5
        backup.AreaData[20] = agentStruct->Area5Byte1;
        backup.AreaData[21] = agentStruct->Area5Byte2;
        backup.AreaData[22] = agentStruct->Area5Byte3;
        backup.AreaData[23] = agentStruct->Area5Byte4;
        backup.AreaData[24] = agentStruct->Area5Byte5;

        // Area 6
        backup.AreaData[25] = agentStruct->Area6Byte1;
        backup.AreaData[26] = agentStruct->Area6Byte2;
        backup.AreaData[27] = agentStruct->Area6Byte3;
        backup.AreaData[28] = agentStruct->Area6Byte4;
        backup.AreaData[29] = agentStruct->Area6Byte5;

        // Area 7
        backup.AreaData[30] = agentStruct->Area7Byte1;
        backup.AreaData[31] = agentStruct->Area7Byte2;
        backup.AreaData[32] = agentStruct->Area7Byte3;
        backup.AreaData[33] = agentStruct->Area7Byte4;
        backup.AreaData[34] = agentStruct->Area7Byte5;

        // Area 8
        backup.AreaData[35] = agentStruct->Area8Byte1;
        backup.AreaData[36] = agentStruct->Area8Byte2;
        backup.AreaData[37] = agentStruct->Area8Byte3;
        backup.AreaData[38] = agentStruct->Area8Byte4;
        backup.AreaData[39] = agentStruct->Area8Byte5;

        // Area 9
        backup.AreaData[40] = agentStruct->Area9Byte1;
        backup.AreaData[41] = agentStruct->Area9Byte2;
        backup.AreaData[42] = agentStruct->Area9Byte3;
        backup.AreaData[43] = agentStruct->Area9Byte4;
        backup.AreaData[44] = agentStruct->Area9Byte5;

        // Area 10
        backup.AreaData[45] = agentStruct->Area10Byte1;
        backup.AreaData[46] = agentStruct->Area10Byte2;
        backup.AreaData[47] = agentStruct->Area10Byte3;
        backup.AreaData[48] = agentStruct->Area10Byte4;
        backup.AreaData[49] = agentStruct->Area10Byte5;

        // Area 11
        backup.AreaData[50] = agentStruct->Area11Byte1;
        backup.AreaData[51] = agentStruct->Area11Byte2;
        backup.AreaData[52] = agentStruct->Area11Byte3;
        backup.AreaData[53] = agentStruct->Area11Byte4;
        backup.AreaData[54] = agentStruct->Area11Byte5;

        return backup;
    }

    public static void RestoreValues(IntPtr agent, AgentSearch.BackupData backup)
    {
        var agentStruct = (AgentSearch*)agent;

        // Restore basic fields
        agentStruct->OnlineStatusLeft = backup.OnlineStatusLeft;
        agentStruct->OnlineStatusRight = backup.OnlineStatusRight;
        agentStruct->ClassSearchRow1 = backup.ClassSearch;
        agentStruct->MinLevel = backup.MinLevel;
        agentStruct->MaxLevel = backup.MaxLevel;
        agentStruct->Language = backup.Language;
        agentStruct->Company = backup.Company;

        // Restore area data
        // Area 1
        agentStruct->Area1Byte1 = backup.AreaData[0];
        agentStruct->Area1Byte2 = backup.AreaData[1];
        agentStruct->Area1Byte3 = backup.AreaData[2];
        agentStruct->Area1Byte4 = backup.AreaData[3];
        agentStruct->Area1Byte5 = backup.AreaData[4];

        // Area 2
        agentStruct->Area2Byte1 = backup.AreaData[5];
        agentStruct->Area2Byte2 = backup.AreaData[6];
        agentStruct->Area2Byte3 = backup.AreaData[7];
        agentStruct->Area2Byte4 = backup.AreaData[8];
        agentStruct->Area2Byte5 = backup.AreaData[9];

        // Area 3
        agentStruct->Area3Byte1 = backup.AreaData[10];
        agentStruct->Area3Byte2 = backup.AreaData[11];
        agentStruct->Area3Byte3 = backup.AreaData[12];
        agentStruct->Area3Byte4 = backup.AreaData[13];
        agentStruct->Area3Byte5 = backup.AreaData[14];

        // Area 4
        agentStruct->Area4Byte1 = backup.AreaData[15];
        agentStruct->Area4Byte2 = backup.AreaData[16];
        agentStruct->Area4Byte3 = backup.AreaData[17];
        agentStruct->Area4Byte4 = backup.AreaData[18];
        agentStruct->Area4Byte5 = backup.AreaData[19];

        // Area 5
        agentStruct->Area5Byte1 = backup.AreaData[20];
        agentStruct->Area5Byte2 = backup.AreaData[21];
        agentStruct->Area5Byte3 = backup.AreaData[22];
        agentStruct->Area5Byte4 = backup.AreaData[23];
        agentStruct->Area5Byte5 = backup.AreaData[24];

        // Area 6
        agentStruct->Area6Byte1 = backup.AreaData[25];
        agentStruct->Area6Byte2 = backup.AreaData[26];
        agentStruct->Area6Byte3 = backup.AreaData[27];
        agentStruct->Area6Byte4 = backup.AreaData[28];
        agentStruct->Area6Byte5 = backup.AreaData[29];

        // Area 7
        agentStruct->Area7Byte1 = backup.AreaData[30];
        agentStruct->Area7Byte2 = backup.AreaData[31];
        agentStruct->Area7Byte3 = backup.AreaData[32];
        agentStruct->Area7Byte4 = backup.AreaData[33];
        agentStruct->Area7Byte5 = backup.AreaData[34];

        // Area 8
        agentStruct->Area8Byte1 = backup.AreaData[35];
        agentStruct->Area8Byte2 = backup.AreaData[36];
        agentStruct->Area8Byte3 = backup.AreaData[37];
        agentStruct->Area8Byte4 = backup.AreaData[38];
        agentStruct->Area8Byte5 = backup.AreaData[39];

        // Area 9
        agentStruct->Area9Byte1 = backup.AreaData[40];
        agentStruct->Area9Byte2 = backup.AreaData[41];
        agentStruct->Area9Byte3 = backup.AreaData[42];
        agentStruct->Area9Byte4 = backup.AreaData[43];
        agentStruct->Area9Byte5 = backup.AreaData[44];

        // Area 10
        agentStruct->Area10Byte1 = backup.AreaData[45];
        agentStruct->Area10Byte2 = backup.AreaData[46];
        agentStruct->Area10Byte3 = backup.AreaData[47];
        agentStruct->Area10Byte4 = backup.AreaData[48];
        agentStruct->Area10Byte5 = backup.AreaData[49];

        // Area 11
        agentStruct->Area11Byte1 = backup.AreaData[50];
        agentStruct->Area11Byte2 = backup.AreaData[51];
        agentStruct->Area11Byte3 = backup.AreaData[52];
        agentStruct->Area11Byte4 = backup.AreaData[53];
        agentStruct->Area11Byte5 = backup.AreaData[54];
    }
}