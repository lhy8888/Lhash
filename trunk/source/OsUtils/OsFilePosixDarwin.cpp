/*
 * OsFile posix (darwin) implementation file
 * Author: Sun Junwen
 * Version: 0.5
 * Provider basic open/close, read, write and
 * attributes functions of file.
 */
#include "stdafx.h"

#include "OsFile.h"

#include <stdlib.h>
#include <stdint.h>
#include <errno.h>
#include <unistd.h>
#include <fcntl.h>
#include <time.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <string>

#include "Common/strhelper.h"

using namespace std;
using namespace sunjwbase;

#define GET_FD_FROM_POINTER(pointer) ((int *)(pointer))

namespace
{
    static const int kNoFollowFlag = O_NOFOLLOW;

    static void CopyOpenErrorText(char *errorBuffer, const char *errorText)
    {
        if (errorBuffer != NULL)
        {
            strlcpy(errorBuffer, errorText, OsFile::ERR_MSG_BUFFER_LEN);
        }
    }

    static bool IsOpenModeCreate(int posixFlag)
    {
        return (posixFlag & O_CREAT) != 0;
    }

    static bool IsRegularFile(const struct stat& st)
    {
        return S_ISREG(st.st_mode) != 0;
    }

    static bool IsSymbolicLink(const struct stat& st)
    {
        return S_ISLNK(st.st_mode) != 0;
    }

    static bool IsSameFileIdentity(const struct stat& lhs, const struct stat& rhs)
    {
        return lhs.st_dev == rhs.st_dev && lhs.st_ino == rhs.st_ino;
    }

    static bool TryGetPathStatus(const std::string& filePath, bool allowMissingPath, struct stat *fileStatus, bool *pathExists)
    {
        if (fileStatus == NULL)
        {
            return false;
        }

        if (pathExists != NULL)
        {
            *pathExists = false;
        }

        struct stat pathStatus;
        if (lstat(filePath.c_str(), &pathStatus) != 0)
        {
            return allowMissingPath && errno == ENOENT;
        }

        if (pathExists != NULL)
        {
            *pathExists = true;
        }

        *fileStatus = pathStatus;
        return true;
    }

    static bool TryValidatePathPolicy(const std::string& filePath, bool allowMissingPath, struct stat *pathStatus, bool *pathExists, char *errorBuffer)
    {
        struct stat fileStatus;
        bool exists = false;
        if (!TryGetPathStatus(filePath, allowMissingPath, &fileStatus, &exists))
        {
            if (errorBuffer != NULL)
            {
                CopyOpenErrorText(errorBuffer, "Cannot inspect this file.");
            }
            return false;
        }

        if (!exists)
        {
            if (pathExists != NULL)
            {
                *pathExists = false;
            }
            return true;
        }

        if (IsSymbolicLink(fileStatus))
        {
            if (errorBuffer != NULL)
            {
                CopyOpenErrorText(errorBuffer, "Refusing to hash a symbolic link.");
            }
            return false;
        }

        if (!IsRegularFile(fileStatus))
        {
            if (errorBuffer != NULL)
            {
                CopyOpenErrorText(errorBuffer, S_ISDIR(fileStatus.st_mode) ? "Cannot open a directory." : "Cannot open this file.");
            }
            return false;
        }

        if (pathStatus != NULL)
        {
            *pathStatus = fileStatus;
        }
        if (pathExists != NULL)
        {
            *pathExists = true;
        }

        return true;
    }

    static bool TryGetCurrentFileStatus(int *fd, const std::string& filePath, struct stat *fileStatus)
    {
        if (fileStatus == NULL)
        {
            return false;
        }

        if (fd != NULL && *fd != -1)
        {
            return fstat(*fd, fileStatus) == 0;
        }

        int temporaryFd = ::open(filePath.c_str(), O_RDONLY | kNoFollowFlag);
        if (temporaryFd == -1)
        {
            return false;
        }

        bool statusRead = fstat(temporaryFd, fileStatus) == 0;
        ::close(temporaryFd);
        return statusRead;
    }

    static bool ValidateOpenedHandleAgainstPathPolicy(int fileHandle, const std::string& expectedPath, const struct stat& expectedPathStatus, bool pathExists, char *errorBuffer)
    {
        struct stat openedStatus;
        if (fstat(fileHandle, &openedStatus) != 0)
        {
            CopyOpenErrorText(errorBuffer, "Cannot inspect this file.");
            return false;
        }

        if (IsSymbolicLink(openedStatus))
        {
            CopyOpenErrorText(errorBuffer, "Refusing to hash a symbolic link.");
            return false;
        }

        if (!IsRegularFile(openedStatus))
        {
            CopyOpenErrorText(errorBuffer, S_ISDIR(openedStatus.st_mode) ? "Cannot open a directory." : "Cannot open this file.");
            return false;
        }

        if (!pathExists)
        {
            return true;
        }

        if (!IsSameFileIdentity(openedStatus, expectedPathStatus))
        {
            CopyOpenErrorText(errorBuffer, "Refusing to hash a path whose resolved handle no longer matches the validated path.");
            return false;
        }

        struct stat reopenedPathStatus;
        if (!TryGetPathStatus(expectedPath, false, &reopenedPathStatus, NULL))
        {
            CopyOpenErrorText(errorBuffer, "Cannot inspect this file.");
            return false;
        }

        if (!IsSameFileIdentity(reopenedPathStatus, openedStatus))
        {
            CopyOpenErrorText(errorBuffer, "Refusing to hash a path whose resolved handle no longer matches the validated path.");
            return false;
        }

        return true;
    }
}

OsFile::OsFile(tstring filePath):
    _filePath(filePath),
    _osfileData(new int(-1)),
    _fileStatus(CLOSED)
{

}

OsFile::~OsFile()
{
    if (_osfileData != NULL)
    {
        if (_fileStatus != CLOSED)
        {
            close();
        }

        delete GET_FD_FROM_POINTER(_osfileData);
    }
}

bool OsFile::isHashTargetAllowed(void *exception)
{
    std::string strFilePath = tstrtostr(_filePath);
    struct stat pathStatus;
    bool pathExists = false;
    return TryValidatePathPolicy(strFilePath, false, &pathStatus, &pathExists, (char *)exception);
}

bool OsFile::open(void *flag, void *exception)
{
    string strFilePath = tstrtostr(_filePath);
    char *pFileExc = (char *)exception;
    int *fd = GET_FD_FROM_POINTER(_osfileData);
    *fd = -1;

    int posixFlag = (int)(uint64_t)flag;
    bool allowMissingPath = IsOpenModeCreate(posixFlag);
    struct stat pathStatus;
    bool pathExists = false;
    if (!TryValidatePathPolicy(strFilePath, allowMissingPath, &pathStatus, &pathExists, pFileExc))
    {
        return false;
    }

    int openFlags = posixFlag | kNoFollowFlag;
    if (IsOpenModeCreate(posixFlag))
    {
        *fd = ::open(strFilePath.c_str(), openFlags, S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH);
    }
    else
    {
        *fd = ::open(strFilePath.c_str(), openFlags);
    }

    if (*fd == -1)
    {
        if (errno == ENOENT)
        {
            CopyOpenErrorText(pFileExc, "File is missing.");
        }
        else if (errno == EISDIR)
        {
            CopyOpenErrorText(pFileExc, "Cannot open a directory.");
        }
        else if (errno == ELOOP)
        {
            CopyOpenErrorText(pFileExc, "Refusing to hash a symbolic link.");
        }
        else
        {
            CopyOpenErrorText(pFileExc, "Cannot open this file.");
        }

        return false;
    }

    if (!ValidateOpenedHandleAgainstPathPolicy(*fd, strFilePath, pathStatus, pathExists, pFileExc))
    {
        ::close(*fd);
        *fd = -1;
        return false;
    }

    return (*fd != -1);
}

bool OsFile::openRead(void *exception/* = NULL*/)
{
    bool ret = false;

    ret = this->open((void *)(O_RDONLY), exception);

    if (ret == true)
    {
        _fileStatus = OPEN_READ;
    }

    return ret;
}

bool OsFile::openReadScan(void *exception/* = NULL*/)
{
    return this->openRead(exception);
}

bool OsFile::openWrite(void *exception/* = NULL*/)
{
    bool ret = false;

    ret = this->open((void *)(O_RDWR | O_CREAT | O_SYNC), exception);

    if (ret == true)
    {
        _fileStatus = OPEN_WRITE;
    }

    return ret;
}

bool OsFile::openReadWrite(void *exception/* = NULL*/)
{
    return this->openWrite(exception);
}

int64_t OsFile::getLength()
{
    int64_t retLength = 0;
    string strFilePath = tstrtostr(_filePath);
    int *fd = GET_FD_FROM_POINTER(_osfileData);

    struct stat st;
    if (TryGetCurrentFileStatus(fd, strFilePath, &st))
    {
        retLength = st.st_size;
    }

    return retLength;
}

bool OsFile::getModifiedTime(void *modifiedTime)
{
    if (modifiedTime == NULL)
    {
        return false;
    }

    struct timespec *darwinfileModTime = (struct timespec *)modifiedTime;

    string strFilePath = tstrtostr(_filePath);
    int *fd = GET_FD_FROM_POINTER(_osfileData);

    struct stat st;
    if (TryGetCurrentFileStatus(fd, strFilePath, &st))
    {
        *darwinfileModTime = st.st_mtimespec;

        return true;
    }

    return false;
}

tstring OsFile::getModifiedTimeFormat()
{
    tstring tstrLastModifiedTime;
    struct timespec ctModifedTime;
    if (this->getModifiedTime((void *)&ctModifedTime))
    {
        time_t ttModifiedTime;
        struct tm *tmModifiedTime;

        ttModifiedTime = ctModifedTime.tv_sec;
        tmModifiedTime = localtime(&ttModifiedTime);

        char szTmBuf[1024] = {0};
        strftime(szTmBuf, 1024, "%Y-%m-%d %H:%M", tmModifiedTime);

        tstrLastModifiedTime = strtotstr(string(szTmBuf));
    }

    return tstrLastModifiedTime;
}

uint64_t OsFile::seek(uint64_t offset, OsFileSeekFrom from)
{
    int posixSeekFlag = SEEK_SET;
    switch(from)
    {
    case OF_SEEK_BEGIN:
        posixSeekFlag = SEEK_SET;
        break;
    case OF_SEEK_CUR:
        posixSeekFlag = SEEK_CUR;
        break;
    case OF_SEEK_END:
        posixSeekFlag = SEEK_END;
        break;
    }

    int *fd = GET_FD_FROM_POINTER(_osfileData);
    if (fd == NULL || *fd == -1)
    {
        return static_cast<uint64_t>(-1);
    }

    off_t seekResult = ::lseek(*fd, static_cast<off_t>(offset), posixSeekFlag);
    return seekResult == static_cast<off_t>(-1) ? static_cast<uint64_t>(-1) : static_cast<uint64_t>(seekResult);
}

int64_t OsFile::read(void *readBuffer, uint32_t bytes)
{
    int *fd = GET_FD_FROM_POINTER(_osfileData);
    if (fd == NULL || *fd == -1)
    {
        return -1;
    }

    return ::read(*fd, readBuffer, bytes);
}

int64_t OsFile::write(void *writeBuffer, uint32_t bytes)
{
    int *fd = GET_FD_FROM_POINTER(_osfileData);
    if (fd == NULL || *fd == -1)
    {
        return -1;
    }

    return ::write(*fd, writeBuffer, bytes);
}

void OsFile::close()
{
    int *fd = GET_FD_FROM_POINTER(_osfileData);

    if (_fileStatus != CLOSED)
    {
        ::close(*fd);
        *fd = -1;
        _fileStatus = CLOSED;
    }
}
