#include "Common/Global.h"
#include "OsUtils/OsFile.h"

#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/stat.h>
#include <unistd.h>

#include <iostream>
#include <cstring>
#include <string>

using namespace sunjwbase;

namespace OsFileDarwinInternal
{
    bool IsSameFileIdentity(const struct stat& lhs, const struct stat& rhs)
    {
        return lhs.st_dev == rhs.st_dev && lhs.st_ino == rhs.st_ino;
    }
}

namespace
{
    bool ExpectTrue(const char *label, bool value)
    {
        if (value)
        {
            std::cout << "[ok] " << label << '\n';
            return true;
        }

        std::cerr << "[fail] " << label << '\n';
        return false;
    }

    bool ExpectFalse(const char *label, bool value)
    {
        return ExpectTrue(label, !value);
    }

    bool WriteTextFile(const std::string& path, const char *text)
    {
        FILE *fp = fopen(path.c_str(), "wb");
        if (fp == NULL)
        {
            return false;
        }

        size_t textLength = strlen(text);
        bool wroteAll = fwrite(text, 1, textLength, fp) == textLength;
        fclose(fp);
        return wroteAll;
    }

    bool RemoveFileIfExists(const std::string& path)
    {
        return unlink(path.c_str()) == 0 || errno == ENOENT;
    }

    bool RemoveDirIfExists(const std::string& path)
    {
        return rmdir(path.c_str()) == 0 || errno == ENOENT;
    }

    std::string CreateTempRoot()
    {
        char tempTemplate[] = "/tmp/lhash_darwin_security_XXXXXX";
        char *tempDir = mkdtemp(tempTemplate);
        return tempDir == NULL ? std::string() : std::string(tempDir);
    }
}

int main()
{
    bool passed = true;

    std::string root = CreateTempRoot();
    if (root.empty())
    {
        std::cerr << "[fail] Could not create temporary working directory." << std::endl;
        return 1;
    }

    std::string regularPath = root + "/regular.txt";
    std::string siblingPath = root + "/sibling.txt";
    std::string symlinkPath = root + "/regular-link.txt";
    std::string directoryPath = root + "/subdir";

    passed &= ExpectTrue("Create regular file", WriteTextFile(regularPath, "darwin-security"));
    passed &= ExpectTrue("Create sibling regular file", WriteTextFile(siblingPath, "darwin-security-sibling"));
    passed &= ExpectTrue("Create leaf directory", mkdir(directoryPath.c_str(), 0700) == 0);
    passed &= ExpectTrue("Create symlink to regular file", symlink(regularPath.c_str(), symlinkPath.c_str()) == 0);

    struct stat regularStat = {};
    struct stat siblingStat = {};
    passed &= ExpectTrue("lstat regular file", lstat(regularPath.c_str(), &regularStat) == 0);
    passed &= ExpectTrue("lstat sibling regular file", lstat(siblingPath.c_str(), &siblingStat) == 0);
    passed &= ExpectTrue("Same file identity matches itself", OsFileDarwinInternal::IsSameFileIdentity(regularStat, regularStat));
    passed &= ExpectFalse("Distinct files do not share a device/inode identity", OsFileDarwinInternal::IsSameFileIdentity(regularStat, siblingStat));

    OsFile regularFile(strtotstr(regularPath));
    passed &= ExpectTrue("Regular file is allowed", regularFile.isHashTargetAllowed());
    passed &= ExpectTrue("Regular file openRead succeeds", regularFile.openRead());
    passed &= ExpectTrue("Regular file length is readable", regularFile.getLength() > 0);
    regularFile.close();

    OsFile symlinkFile(strtotstr(symlinkPath));
    passed &= ExpectFalse("Symlink path is rejected", symlinkFile.isHashTargetAllowed());
    passed &= ExpectFalse("Symlink path openRead fails", symlinkFile.openRead());

    OsFile directoryFile(strtotstr(directoryPath));
    passed &= ExpectFalse("Directory path is rejected", directoryFile.isHashTargetAllowed());
    passed &= ExpectFalse("Directory openRead fails", directoryFile.openRead());

    passed &= ExpectTrue("Cleanup symlink", RemoveFileIfExists(symlinkPath));
    passed &= ExpectTrue("Cleanup regular file", RemoveFileIfExists(regularPath));
    passed &= ExpectTrue("Cleanup sibling file", RemoveFileIfExists(siblingPath));
    passed &= ExpectTrue("Cleanup directory", RemoveDirIfExists(directoryPath));
    passed &= ExpectTrue("Cleanup root", RemoveDirIfExists(root));

    if (!passed)
    {
        std::cerr << "lhash_darwin_security failed." << std::endl;
        return 1;
    }

    std::cout << "lhash_darwin_security passed." << std::endl;
    return 0;
}
