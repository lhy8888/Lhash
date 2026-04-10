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
#include "Common/HashResult.h"
#include "MacUtils.h"

using namespace std;
using namespace sunjwbase;

UIBridgeMacSwift::UIBridgeMacSwift(MainViewController *mainViewController)
:_mainViewControllerPtr(mainViewController)
{
}

UIBridgeMacSwift::~UIBridgeMacSwift()
{
}

void UIBridgeMacSwift::lockBridgeData()
{
    //MainViewController *mainViewController = _mainViewControllerPtr.get();
    //mainViewController.mainMtx->lock();
}

void UIBridgeMacSwift::unlockBridgeData()
{
    //MainViewController *mainViewController = _mainViewControllerPtr.get();
    //mainViewController.mainMtx->unlock();
}

void UIBridgeMacSwift::handleJobPreparingEvent()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onPreparingCalc];
    });
}

void UIBridgeMacSwift::handleJobPreparationFinishedEvent()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onRemovePreparingCalc];
    });
}

void UIBridgeMacSwift::handleJobCancelledEvent()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onCalcStop];
    });
}

void UIBridgeMacSwift::handleJobCompletedEvent()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onCalcFinish];
    });
}

void UIBridgeMacSwift::handleFileResultProgressEvent(const HashResult& result,
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

int UIBridgeMacSwift::getProgressValueMax()
{
    return 200;
}

void UIBridgeMacSwift::handleFileProgressEvent(int value)
{
}

void UIBridgeMacSwift::handleTotalProgressEvent(int value)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        MainViewController *mainViewController = _mainViewControllerPtr.get();
        [mainViewController onUpdateProgWhole:value];
    });
}

void UIBridgeMacSwift::handleFileCalculatedEvent()
{
}

void UIBridgeMacSwift::handleFileFinishedEvent()
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
        std::string algorithmId = tstrtostr(ResolveHashDigestResultAlgorithmId(digestResult));
        if (algorithmId.empty())
        {
            continue;
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
        }
    }

    return resultDataSwift;
}
