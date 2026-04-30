// dllmain.h : 模块类的声明。

class CLHashShlExtModule : public CAtlDllModuleT< CLHashShlExtModule >
{
public :
	DECLARE_LIBID(LIBID_LHashShlExtLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_LHASHSHLEXT, "{AA6F714E-CBFA-4DA6-B363-B5692D2E5BA0}")
};

extern class CLHashShlExtModule _AtlModule;

