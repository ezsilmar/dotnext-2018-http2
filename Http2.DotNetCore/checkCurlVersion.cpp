#include <curl/curl.h>
#include <stdio.h>

int main() 
{
	curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
	int version = info->version_num;
	printf("Version is %x\n", version);
	return 0;
}