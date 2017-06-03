using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;

namespace RemoveDriveByLetter
{
    // Converted from: http://www.codeproject.com/Articles/13839/How-to-Prepare-a-USB-Drive-for-Safe-Removal

    public class RemoveDriveTools
    {
        [StructLayout( LayoutKind.Sequential )]
        struct STORAGE_DEVICE_NUMBER
        {
            public int DeviceType;
            public int DeviceNumber;
            public int PartitionNumber;
        };

        enum DriveType : uint
        {
            /// <summary>The drive type cannot be determined.</summary>
            DRIVE_UNKNOWN = 0,      //DRIVE_UNKNOWN
            /// <summary>The root path is invalid, for example, no volume is mounted at the path.</summary>
            DRIVE_NO_ROOT_DIR = 1,  //DRIVE_NO_ROOT_DIR
            /// <summary>The drive is a type that has removable media, for example, a floppy drive or removable hard disk.</summary>
            DRIVE_REMOVABLE = 2,    //DRIVE_REMOVABLE
            /// <summary>The drive is a type that cannot be removed, for example, a fixed hard drive.</summary>
            DRIVE_FIXED = 3,        //DRIVE_FIXED
            /// <summary>The drive is a remote (network) drive.</summary>
            DRIVE_REMOTE = 4,       //DRIVE_REMOTE
            /// <summary>The drive is a CD-ROM drive.</summary>
            DRIVE_CDROM = 5,        //DRIVE_CDROM
            /// <summary>The drive is a RAM disk.</summary>
            DRIVE_RAMDISK = 6       //DRIVE_RAMDISK
        }

        const string GUID_DEVINTERFACE_VOLUME = "53f5630d-b6bf-11d0-94f2-00a0c91efb8b";
        const string GUID_DEVINTERFACE_DISK = "53f56307-b6bf-11d0-94f2-00a0c91efb8b";
        const string GUID_DEVINTERFACE_FLOPPY = "53f56311-b6bf-11d0-94f2-00a0c91efb8b";
        const string GUID_DEVINTERFACE_CDROM = "53f56308-b6bf-11d0-94f2-00a0c91efb8b";

        const int INVALID_HANDLE_VALUE = -1;
        const int GENERIC_READ = unchecked( ( int ) 0x80000000 );
        const int GENERIC_WRITE = unchecked( ( int ) 0x40000000 );
        const int FILE_SHARE_READ = unchecked( ( int ) 0x00000001 );
        const int FILE_SHARE_WRITE = unchecked( ( int ) 0x00000002 );
        const int OPEN_EXISTING = unchecked( ( int ) 3 );
        const int FSCTL_LOCK_VOLUME = unchecked( ( int ) 0x00090018 );
        const int FSCTL_DISMOUNT_VOLUME = unchecked( ( int ) 0x00090020 );
        const int IOCTL_STORAGE_EJECT_MEDIA = unchecked( ( int ) 0x002D4808 );
        const int IOCTL_STORAGE_MEDIA_REMOVAL = unchecked( ( int ) 0x002D4804 );
        const int IOCTL_STORAGE_GET_DEVICE_NUMBER = unchecked( ( int ) 0x002D1080 );

        const int ERROR_NO_MORE_ITEMS = 259;
        const int ERROR_INSUFFICIENT_BUFFER = 122;
        const int ERROR_INVALID_DATA = 13;

        [DllImport( "kernel32.dll" )]
        static extern DriveType GetDriveType( [MarshalAs( UnmanagedType.LPStr )] string lpRootPathName );

        [DllImport( "kernel32.dll" )]
        static extern uint QueryDosDevice( string lpDeviceName, StringBuilder lpTargetPath, int ucchMax );

        [DllImport( "kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true )]
        static extern IntPtr CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            int dwShareMode,
            IntPtr lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile );

        [DllImport( "kernel32.dll", ExactSpelling = true, SetLastError = true )]
        static extern bool CloseHandle( IntPtr handle );

        [DllImport( "kernel32.dll", ExactSpelling = true, SetLastError = true )]
        static extern bool DeviceIoControl(
            IntPtr hDevice,
            int dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped );

        // from setupapi.h
        const int DIGCF_PRESENT = ( 0x00000002 );
        const int DIGCF_DEVICEINTERFACE = ( 0x00000010 );

        [StructLayout( LayoutKind.Sequential )]
        class SP_DEVINFO_DATA
        {
            public int cbSize = Marshal.SizeOf( typeof( SP_DEVINFO_DATA ) );
            public Guid classGuid = Guid.Empty; // temp
            public int devInst = 0; // dumy
            public int reserved = 0;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 2 )]
        struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            public short devicePath;
        }

        [StructLayout( LayoutKind.Sequential )]
        class SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize = Marshal.SizeOf( typeof( SP_DEVICE_INTERFACE_DATA ) );
            public Guid interfaceClassGuid = Guid.Empty; // temp
            public int flags = 0;
            public int reserved = 0;
        }

        [DllImport( "setupapi.dll" )]
        static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            int enumerator,
            IntPtr hwndParent,
            int flags );

        [DllImport( "setupapi.dll", SetLastError = true, CharSet = CharSet.Auto )]
        static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet,
            SP_DEVINFO_DATA deviceInfoData,
            ref Guid interfaceClassGuid,
            int memberIndex,
            SP_DEVICE_INTERFACE_DATA deviceInterfaceData );

        [DllImport( "setupapi.dll", SetLastError = true, CharSet = CharSet.Auto )]
        static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr deviceInfoSet,
            SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize,
            ref int requiredSize,
            SP_DEVINFO_DATA deviceInfoData );

        [DllImport( "setupapi.dll" )]
        static extern uint SetupDiDestroyDeviceInfoList(
            IntPtr deviceInfoSet );

        [DllImport( "setupapi.dll" )]
        static extern int CM_Get_Parent(
            ref int pdnDevInst,
            int dnDevInst,
            int ulFlags );

        [DllImport( "setupapi.dll" )]
        static extern int CM_Request_Device_Eject(
            int dnDevInst,
            out PNP_VETO_TYPE pVetoType,
            StringBuilder pszVetoName,
            int ulNameLength,
            int ulFlags );

        [DllImport( "setupapi.dll", EntryPoint = "CM_Request_Device_Eject" )]
        static extern int CM_Request_Device_Eject_NoUi(
            int dnDevInst,
            IntPtr pVetoType,
            StringBuilder pszVetoName,
            int ulNameLength,
            int ulFlags );

        enum PNP_VETO_TYPE
        {
            Ok,
            TypeUnknown,
            LegacyDevice,
            PendingClose,
            WindowsApp,
            WindowsService,
            OutstandingOpen,
            Device,
            Driver,
            IllegalDeviceRequest,
            InsufficientPower,
            NonDisableable,
            LegacyDriver,
            InsufficientRights
        }

        /// <summary>
        /// Call with "X:" or similar
        /// </summary>
        /// <param name="driveCharWithColon"></param>
        /// <returns></returns>
        public static bool RemoveDrive( string driveCharWithColon )
        {
            // open the storage volume
            IntPtr hVolume = CreateFile( @"\\.\" + driveCharWithColon, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero );
            if ( hVolume.ToInt32( ) == -1 ) return false;

            // get the volume's device number
            long DeviceNumber = GetDeviceNumber( hVolume );
            if ( DeviceNumber == -1 ) return false;

            // get the drive type which is required to match the device numbers correctely
            string rootPath = driveCharWithColon + "\\";        
            DriveType driveType = GetDriveType( rootPath );

            // get the dos device name (like \device\floppy0) to decide if it's a floppy or not - who knows a better way?
            StringBuilder pathInformation = new StringBuilder( 250 );
            uint res = QueryDosDevice( driveCharWithColon, pathInformation, 250 );
            if ( res == 0 ) return false;

            // get the device instance handle of the storage volume by means of a SetupDi enum and matching the device number
            long DevInst = GetDrivesDevInstByDeviceNumber( DeviceNumber, driveType, pathInformation.ToString( ) );
            if ( DevInst == 0 ) return false;

            // get drives's parent, e.g. the USB bridge, the SATA port, an IDE channel with two drives!
            int DevInstParent = 0;
            CM_Get_Parent( ref DevInstParent, ( int ) DevInst, 0 );

            for ( int tries=1; tries <= 3; tries++ )  // sometimes we need some tries...
            {
                int r = CM_Request_Device_Eject_NoUi( DevInstParent, IntPtr.Zero, null, 0, 0 );
                if ( r == 0 ) return true;
                Thread.Sleep( 500 );
            }
            return false;
        }

        static long GetDeviceNumber( IntPtr handle )
        {
            // get the volume's device number
            long DeviceNumber = -1;
            int size = 0x400; // some big size
            IntPtr buffer = Marshal.AllocHGlobal( size );
            int bytesReturned = 0;

            try
            {
                DeviceIoControl( handle, IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, buffer, size, out bytesReturned, IntPtr.Zero );
            }
            finally
            {
                CloseHandle( handle );
            }

            if ( bytesReturned > 0 )
            {
                STORAGE_DEVICE_NUMBER sdn = ( STORAGE_DEVICE_NUMBER ) Marshal.PtrToStructure( buffer, typeof( STORAGE_DEVICE_NUMBER ) );
                DeviceNumber = sdn.DeviceNumber;
            }
            Marshal.FreeHGlobal( buffer );

            return DeviceNumber;
        }

        // Returns the device instance handle of a storage volume or 0 on error
        static long GetDrivesDevInstByDeviceNumber( long DeviceNumber, DriveType DriveType, string dosDeviceName )
        {
            bool IsFloppy = dosDeviceName.Contains( "\\Floppy" ); // who knows a better way?
            Guid guid;

            switch ( DriveType )
            {
                case DriveType.DRIVE_REMOVABLE:
                    if ( IsFloppy ) guid = new Guid( GUID_DEVINTERFACE_FLOPPY );
                    else guid = new Guid( GUID_DEVINTERFACE_DISK );
                    break;
                case DriveType.DRIVE_FIXED:
                    guid = new Guid( GUID_DEVINTERFACE_DISK );
                    break;
                case DriveType.DRIVE_CDROM:
                    guid = new Guid( GUID_DEVINTERFACE_CDROM );
                    break;
                default:
                    return 0;
            }

            // Get device interface info set handle for all devices attached to system
            IntPtr hDevInfo = SetupDiGetClassDevs( ref guid, 0, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE );

            if ( hDevInfo.ToInt32( ) == INVALID_HANDLE_VALUE ) throw new Win32Exception( Marshal.GetLastWin32Error( ) );

            // Retrieve a context structure for a device interface of a device information set
            int dwIndex = 0;

            while ( true )
            {
                SP_DEVICE_INTERFACE_DATA interfaceData = new SP_DEVICE_INTERFACE_DATA( );
                if ( !SetupDiEnumDeviceInterfaces( hDevInfo, null, ref guid, dwIndex, interfaceData ) )
                {
                    int error = Marshal.GetLastWin32Error( );
                    if ( error != ERROR_NO_MORE_ITEMS ) throw new Win32Exception( error );
                    break;
                }

                SP_DEVINFO_DATA devData = new SP_DEVINFO_DATA( );
                int size = 0;
                if ( !SetupDiGetDeviceInterfaceDetail( hDevInfo, interfaceData, IntPtr.Zero, 0, ref size, devData ) )
                {
                    int error = Marshal.GetLastWin32Error( );
                    if ( error != ERROR_INSUFFICIENT_BUFFER ) throw new Win32Exception( error );
                }

                IntPtr buffer = Marshal.AllocHGlobal( size );
                SP_DEVICE_INTERFACE_DETAIL_DATA detailData = new SP_DEVICE_INTERFACE_DETAIL_DATA( );
                detailData.cbSize = Marshal.SizeOf( typeof( SP_DEVICE_INTERFACE_DETAIL_DATA ) );
                Marshal.StructureToPtr( detailData, buffer, false );

                if ( !SetupDiGetDeviceInterfaceDetail( hDevInfo, interfaceData, buffer, size, ref size, devData ) )
                {
                    Marshal.FreeHGlobal( buffer );
                    throw new Win32Exception( Marshal.GetLastWin32Error( ) );
                }

                IntPtr pDevicePath = ( IntPtr ) ( ( int ) buffer + Marshal.SizeOf( typeof( int ) ) );
                string devicePath = Marshal.PtrToStringAuto( pDevicePath );
                Marshal.FreeHGlobal( buffer );

                // open the disk or cdrom or floppy
                IntPtr hDrive = CreateFile( devicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero );
                if ( hDrive.ToInt32( ) != INVALID_HANDLE_VALUE )
                {
                    // get its device number
                    long driveDeviceNumber = GetDeviceNumber( hDrive );
                    if ( DeviceNumber == driveDeviceNumber )   // match the given device number with the one of the current device
                    {
                        //CloseHandle( hDrive );
                        SetupDiDestroyDeviceInfoList( hDevInfo );
                        return devData.devInst;
                    }
                    //CloseHandle( hDrive );
                }
                dwIndex++;
            }

            SetupDiDestroyDeviceInfoList( hDevInfo );
            return 0;
        }

        static void Test( )
        {
            bool ok = RemoveDrive( "H:" );
        }
    }
}
