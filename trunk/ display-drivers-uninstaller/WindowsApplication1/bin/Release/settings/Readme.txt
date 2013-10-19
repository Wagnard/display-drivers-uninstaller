If you dont know what you are doing, you should not modify the files under settings folders. You have been warned...

"driverfiles.cfg "
Is meant for the removal of driver files from Windows\System32\drivers  \System32  \SysWOW64  and the registry location of (pnplockdownfiles)
Work as "startwith"


"services.cfg:
Is meant for the removal of services.
Work as exact match

"classroot.cfg"  Not meant for noobs.
Is meant for the removal of registry entry that contains the value in the .cfg in HKCR. (also include Wow6432Node)
This also remove (in most case) what is linked to the HKCR --> CLSID --> AppID
                                                                     --> TypeLib
Work as contain.

"clsidleftover.cfg" Not meant for noobs.
is meant to take care of what classroot.cfg could do on CLSID Inprocserver32(and the Wow6432node part) because there wasnt any
known logic between HKCR and CLSID
Work as contain