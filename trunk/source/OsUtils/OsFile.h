/*
 * OsFile header file
 * Author: Sun Junwen
 * Version: 0.5
 * Provider basic open/close, read, write and
 * attributes functions of file.
 */
#ifndef _OS_FILE_H_
#define _OS_FILE_H_

#include <stdlib.h>
#include <stdint.h>
#include <memory>
#include "Common/strhelper.h"

namespace sunjwbase
{
	class OsFile
	{
	public:
		static const uint32_t ERR_MSG_BUFFER_LEN = 1024;

		enum OsFileStatus
		{
			CLOSED = 0, OPEN_READ, OPEN_WRITE, OPEN_READWRITE
		};

		enum OsFileSeekFrom
		{
			OF_SEEK_BEGIN = 0, OF_SEEK_CUR, OF_SEEK_END
		};

		explicit OsFile(sunjwbase::tstring filePath);
		~OsFile();

		// Open
		bool openRead(void *exception = NULL);
		bool openReadScan(void *exception = NULL);
		bool openWrite(void *exception = NULL);
		bool openReadWrite(void *exception = NULL);

		// Attributes
		bool isHashTargetAllowed(void *exception = NULL);
		int64_t getLength();
		bool getModifiedTime(void *modifiedTime);
		sunjwbase::tstring getModifiedTimeFormat();
		const sunjwbase::tstring& getPath() const { return _filePath; }

		// Operation
		uint64_t seek(uint64_t offset, OsFileSeekFrom from);
		int64_t read(void *readBuffer, uint32_t bytes);
		int64_t write(void *writeBuffer, uint32_t bytes);
		// Verify a complete sequential read before publishing a file digest.
		bool validateReadConsistency(sunjwbase::tstring *errorText = NULL) const;

		// Close
		void close();

	private:
		bool open(void *flag, void *exception);
		bool captureReadConsistency(void *exception);
		struct ReadConsistencyState;

		sunjwbase::tstring	_filePath;
		std::unique_ptr<ReadConsistencyState> _readConsistency;
		void				*_osfileData;
		OsFileStatus		_fileStatus;
	};

}

#endif // _OS_FILE_H_

