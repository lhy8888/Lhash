//
//  UIBridgeMacSwift.mm
//  fHash
//
//  Created by Sun Junwen on 2023/12/7.
//  Copyright © 2023 Sun Junwen. All rights reserved.
//

#import "UIBridgeMacSwift.h"

#include <stdlib.h>
#include <string>
#include <dispatch/dispatch.h>

#import <Cocoa/Cocoa.h>
#import "fHash-Swift-Header.h"

#include "Common/strhelper.h"
#include "Common/Utils.h"
#include "MacUtils.h"
#include "Common/Global.h"

using namespace std;
using namespace sunjwbase;

UIBridgeMacSwift::UIBridgeMacSwift(MainViewController *mainViewController)
:_mainViewControllerPtr(mainViewController)
{
}

UIBridgeMacSwift::~UIBridgeMacSwift()
{
}

void UIBridgeMacSwift::lockData()
{
    //MainViewController *mainViewController = _mainViewControllerPtr.get();
    //mainViewController.mainMtx->lock();
}

void UIBridgeMacSwift::unlockData()
{
    //MainViewController *mainViewController = _mainViewControllerPtr.get();
    //mainViewController.mainMtx->unlock();
}

void UIBridgeMacSwift::onJobPreparing()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onPreparingCalc];
    });
}

void UIBridgeMacSwift::onJobPreparationFinished()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onRemovePreparingCalc];
    });
}

void UIBridgeMacSwift::onJobCancelled()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onCalcStop];
    });
}

void UIBridgeMacSwift::onJobCompleted()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onCalcFinish];
    });
}

void UIBridgeMacSwift::onFileResultEvent(const HashResult& result,
                                         ProgressEventType eventType,
                                         bool uppercaseDigest)
{
    ResultDataSwift *resultSwift = UIBridgeMacSwift::ConvertHashResultToSwift(result);
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        switch (eventType)
        {
            case PROGRESS_EVENT_FILE_STARTED:
                [mainViewController onShowFileName:resultSwift];
                break;
            case PROGRESS_EVENT_FILE_META_READY:
                [mainViewController onShowFileMeta:resultSwift];
                break;
            case PROGRESS_EVENT_FILE_HASH_READY:
                [mainViewController onShowFileHash:resultSwift uppercase:uppercaseDigest];
                break;
            case PROGRESS_EVENT_FILE_FAILED:
                [mainViewController onShowFileErr:resultSwift];
                break;
            default:
                break;
        }
    });
}

int UIBridgeMacSwift::queryProgressMax()
{
    return 200;
}

void UIBridgeMacSwift::onFileProgressValue(int value)
{
}

void UIBridgeMacSwift::onTotalProgressValue(int value)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onUpdateProgWhole:value];
    });
}

void UIBridgeMacSwift::onFileCalculated()
{
}

void UIBridgeMacSwift::onFileFinished()
{
}

ResultDataSwift *UIBridgeMacSwift::ConvertHashResultToSwift(const HashResult& result)
{
    ResultDataSwift *resultDataSwift = [[ResultDataSwift alloc] init];
    switch (result.state)
    {
        case ResultState::RESULT_NONE:
            resultDataSwift.state = ResultDataSwift.RESULT_NONE;
            break;
        case ResultState::RESULT_PATH:
            resultDataSwift.state = ResultDataSwift.RESULT_PATH;
            break;
        case ResultState::RESULT_META:
            resultDataSwift.state = ResultDataSwift.RESULT_META;
            break;
        case ResultState::RESULT_ALL:
            resultDataSwift.state = ResultDataSwift.RESULT_ALL;
            break;
        case ResultState::RESULT_ERROR:
            resultDataSwift.state = ResultDataSwift.RESULT_ERROR;
            break;
    }
    resultDataSwift.strPath = MacUtils::ConvertUTF8StringToNSString(tstrtostr(result.path));
    resultDataSwift.ulSize = result.meta.size;
    resultDataSwift.strMDate = MacUtils::ConvertUTF8StringToNSString(tstrtostr(result.meta.modifiedDate));
    resultDataSwift.strVersion = MacUtils::ConvertUTF8StringToNSString(tstrtostr(result.meta.version));
    resultDataSwift.strError = MacUtils::ConvertUTF8StringToNSString(tstrtostr(result.error));

    for (size_t digestIndex = 0; digestIndex < result.digests.size(); ++digestIndex)
    {
        const HashDigestResult& digestResult = result.digests[digestIndex];
        NSString *digestValue = MacUtils::ConvertUTF8StringToNSString(tstrtostr(digestResult.value));
        std::string algorithmId = tstrtostr(NormalizeHashAlgorithmId(digestResult.algorithmId));
        if (algorithmId.empty())
        {
            algorithmId = tstrtostr(NormalizeHashAlgorithmId(digestResult.stableName));
        }

        if (algorithmId == "md5")
        {
            resultDataSwift.strMD5 = digestValue;
            continue;
        }
        if (algorithmId == "sha1")
        {
            resultDataSwift.strSHA1 = digestValue;
            continue;
        }
        if (algorithmId == "sha256")
        {
            resultDataSwift.strSHA256 = digestValue;
            continue;
        }
        if (algorithmId == "sha512")
        {
            resultDataSwift.strSHA512 = digestValue;
            continue;
        }

        switch (digestResult.type)
        {
        case RESULT_DIGEST_MD5:
            resultDataSwift.strMD5 = digestValue;
            break;
        case RESULT_DIGEST_SHA1:
            resultDataSwift.strSHA1 = digestValue;
            break;
        case RESULT_DIGEST_SHA256:
            resultDataSwift.strSHA256 = digestValue;
            break;
        case RESULT_DIGEST_SHA512:
            resultDataSwift.strSHA512 = digestValue;
            break;
        default:
            break;
        }
    }

    return resultDataSwift;
}
