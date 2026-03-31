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

    virtual void preparingCalc();
    virtual void removePreparingCalc();
    virtual void calcStop();
    virtual void calcFinish();

    virtual void showFileName(const HashResult& result);
    virtual void showFileMeta(const HashResult& result);
    virtual void showFileHash(const HashResult& result, bool uppercase);
    virtual void showFileErr(const HashResult& result);

    virtual int getProgMax();
    virtual void updateProg(int value);
    virtual void updateProgWhole(int value);

    virtual void fileCalcFinish();
    virtual void fileFinish();

    /**
     * Convert HashResult to ResultDataSwift
     */
    static ResultDataSwift *ConvertHashResultToSwift(const HashResult& result);

private:
    MacUtils::ObjcWeakPtr<MainViewController> _mainViewControllerPtr;
};

#endif

