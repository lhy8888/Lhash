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

class UIBridgeMacSwift: public HashEngineBridge
{
public:
    UIBridgeMacSwift(MainViewController *mainViewController);
    virtual ~UIBridgeMacSwift();

    virtual void lockData();
    virtual void unlockData();

    virtual void onJobPreparing();
    virtual void onJobPreparationFinished();
    virtual void onJobCancelled();
    virtual void onJobCompleted();

    virtual void onFileResultEvent(const HashResult& result,
                                   ProgressEventType eventType,
                                   bool uppercaseDigest);

    virtual int queryProgressMax();
    virtual void onFileProgressValue(int value);
    virtual void onTotalProgressValue(int value);

    virtual void onFileCalculated();
    virtual void onFileFinished();

    /**
     * Convert HashResult to ResultDataSwift
     */
    static ResultDataSwift *ConvertHashResultToSwift(const HashResult& result);

private:
    MacUtils::ObjcWeakPtr<MainViewController> _mainViewControllerPtr;
};

#endif

