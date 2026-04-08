//
//  UIBridgeMacSwift.h
//  fHashMacUI
//
//  Created by Sun Junwen on 2023/12/7.
//  Copyright © 2023 Sun Junwen. All rights reserved.
//

#ifndef _UI_BRIDGE_MAC_SWIFT_
#define _UI_BRIDGE_MAC_SWIFT_

#include "OsUtils/OsThread.h"
#include "Adapters/UiBridge/HashEngineBridge.h"
#include "Common/HashResult.h"

#import "MacUtils.h"

@class MainViewController;
@class ResultDataSwift;

class UIBridgeMacSwift: public HashUiBridgeAdapter
{
public:
    UIBridgeMacSwift(MainViewController *mainViewController);
    virtual ~UIBridgeMacSwift();

    virtual void lockBridgeData();
    virtual void unlockBridgeData();

    virtual void handleJobPreparingEvent();
    virtual void handleJobPreparationFinishedEvent();
    virtual void handleJobCancelledEvent();
    virtual void handleJobCompletedEvent();

    virtual void handleFileResultProgressEvent(const HashResult& result,
                                               ProgressEventType eventType,
                                               bool uppercaseDigest);

    virtual int getProgressValueMax();
    virtual void handleFileProgressEvent(int value);
    virtual void handleTotalProgressEvent(int value);

    virtual void handleFileCalculatedEvent();
    virtual void handleFileFinishedEvent();

    /**
     * Convert HashResult to ResultDataSwift
     */
    static ResultDataSwift *ConvertHashResultToSwift(const HashResult& result);

private:
    MacUtils::ObjcWeakPtr<MainViewController> _mainViewControllerPtr;
};

#endif

